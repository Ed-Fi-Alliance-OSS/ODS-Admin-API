// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using EdFi.Ods.AdminApi.AdminConsole.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminConsole.DBTests;

public static class Testing
{
    private static IConfigurationRoot _config;

    public static IConfiguration Configuration()
    {
        _config ??= new ConfigurationBuilder()
               .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
        return _config;
    }

    public static string AdminConnectionString { get { return Configuration().GetConnectionString("EdFi_Admin"); } }

    public static DbContextOptions GetDbContextOptions(string connectionString)
    {
        var builder = new DbContextOptionsBuilder();
        builder.UseSqlServer(connectionString);
        return builder.Options;
    }

    public static IOptions<AppSettings> GetAppSettings()
    {
        AppSettings appSettings = new()
        {
            DatabaseEngine = DatabaseEngineEnum.PostgreSql.ToString()
        };
        return Options.Create(appSettings);
    }

    public static IOptionsSnapshot<AppSettingsFile> GetOptionsSnapshot()
    {
        var appSettingsFile = new AppSettingsFile
        {
            AppSettings = new AppSettings
            {
                DefaultPageSizeOffset = 0,
                DefaultPageSizeLimit = 100,
                DatabaseEngine = "PostgreSql",
                EncryptionKey = "TDMyNH0lJmo7aDRnNXYoSmAwSXQpV09nbitHSWJTKn0=",
                MultiTenancy = true,
                PreventDuplicateApplications = true,
                EnableAdminConsoleAPI = true
            },
            SwaggerSettings = new SwaggerSettings(),
            AdminConsoleSettings = new AdminConsoleSettings
            {
                VendorCompany = "Ed-Fi Administrative Tools",
                ApplicationName = "Ed-Fi Health Check"
            },
            EdFiApiDiscoveryUrl = "https://api.ed-fi.org/v7.2/api/",
            ConnectionStrings = new[] { "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;" },
            Tenants = new Dictionary<string, TenantSettings>
            {
                {
                    "tenant1", new TenantSettings
                    {
                        ConnectionStrings = new Dictionary<string, string>
                        {
                            { "EdFi_Admin", "Data Source=.\\SQLEXPRESS;Initial Catalog=EdFi_Admin_1;Integrated Security=True;Encrypt=false;Trusted_Connection=true" },
                            { "EdFi_Security", "Data Source=.\\SQLEXPRESS;Initial Catalog=EdFi_Security;Integrated Security=True;Encrypt=false;Trusted_Connection=true" }
                        },
                        EdFiApiDiscoveryUrl = "https://api.ed-fi.org/v7.2/api/"
                    }
                },
                {
                    "tenant2", new TenantSettings
                    {
                        ConnectionStrings = new Dictionary<string, string>
                        {
                            { "EdFi_Admin", "Data Source=.\\SQLEXPRESS;Initial Catalog=EdFi_Admin_1;Integrated Security=True;Encrypt=false;Trusted_Connection=true" },
                            { "EdFi_Security", "Data Source=.\\SQLEXPRESS;Initial Catalog=EdFi_Security;Integrated Security=True;Encrypt=false;Trusted_Connection=true" }
                        },
                        EdFiApiDiscoveryUrl = "https://api.ed-fi.org/v7.2/api/"
                    }
                }
            },
            Testing = new TestingSettings
            {
                InjectException = false
            }
        };

        var optionsSnapshot = A.Fake<IOptionsSnapshot<AppSettingsFile>>();
        A.CallTo(() => optionsSnapshot.Value).Returns(appSettingsFile);
        return optionsSnapshot;
    }

    public static IOptions<AdminConsoleSettings> GetAdminConsoleSettings()
    {
        AdminConsoleSettings appSettings = new()
        {
            VendorCompany = "Ed-Fi Administrative Tools",
            ApplicationName = "Ed-Fi Health Check"
        };
        return Options.Create(appSettings);
    }

    public static IOptionsMonitor<TestingSettings> GetTestingSettings(bool injectException = false)
    {
        TestingSettings testingSettings = new()
        {
            InjectException = injectException,
        };
        var optionsMonitor = A.Fake<IOptionsMonitor<TestingSettings>>();
        A.CallTo(() => optionsMonitor.CurrentValue).Returns(testingSettings);
        return optionsMonitor;
    }

    public static IEncryptionKeyResolver GetEncryptionKeyResolver()
    {
        var encryptionKeyResolver = A.Fake<IEncryptionKeyResolver>();
        A.CallTo(() => encryptionKeyResolver.GetEncryptionKey()).Returns("TDMyNH0lJmo7aDRnNXYoSmAwSXQpV09nbitHSWJTKn0=");
        return encryptionKeyResolver;
    }
}
