// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

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

    public static IOptions<AdminConsoleSettings> GetAdminConsoleSettings()
    {
        AdminConsoleSettings appSettings = new()
        {
            VendorCompany = "Ed-Fi Administrative Tools",
            ApplicationName = "Ed-Fi Health Check"
        };
        return Options.Create(appSettings);
    }

    public static IEncryptionKeyResolver GetEncryptionKeyResolver()
    {
        var encryptionKeyResolver = A.Fake<IEncryptionKeyResolver>();
        A.CallTo(() => encryptionKeyResolver.GetEncryptionKey()).Returns("TDMyNH0lJmo7aDRnNXYoSmAwSXQpV09nbitHSWJTKn0=");
        return encryptionKeyResolver;
    }
}
