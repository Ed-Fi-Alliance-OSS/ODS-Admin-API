// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V1.DBTests;

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

    public static string SecurityConnectionString { get { return Configuration().GetConnectionString("EdFi_Security"); } }

    public static int DefaultPageSizeOffset => (int)Configuration().GetValue(typeof(int), "AppSettings:DefaultPageSizeOffset");

    public static int DefaultPageSizeLimit => (int)Configuration().GetValue(typeof(int), "AppSettings:DefaultPageSizeLimit");

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
            DefaultPageSizeOffset = DefaultPageSizeOffset,
            DefaultPageSizeLimit = DefaultPageSizeLimit
        };
        IOptions<AppSettings> options = Options.Create(appSettings);
        return options;
    }
}
