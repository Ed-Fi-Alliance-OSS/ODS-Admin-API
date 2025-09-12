// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Tenants;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.AdminConsole;

public static class ServicesBuilderExtension
{
    public static void AddAdminConsoleServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<TenantBackgroundService>();

        builder.Services.Configure<AppSettingsFile>(builder.Configuration);

        builder.Services.Configure<AdminConsoleSettings>(builder.Configuration.GetSection("AdminConsoleSettings"));

        builder.Services.AddTransient<IAdminConsoleTenantsService, TenantService>();

        builder.RegisterAdminConsoleServices();

    }

    private static void RegisterAdminConsoleServices(this WebApplicationBuilder builder)
    {
        foreach (var type in typeof(IMarkerForEdFiAdminConsoleManagement).Assembly.GetTypes())
        {
            if (type.IsClass && !type.IsAbstract && (type.IsPublic || type.IsNestedPublic))
            {
                var concreteClass = type;

                var interfaces = concreteClass.GetInterfaces().ToArray();

                if (concreteClass.Namespace != null)
                {
                    if (!concreteClass.Namespace.EndsWith("Commands") &&
                        !concreteClass.Namespace.EndsWith("Queries"))
                    {
                        continue;
                    }

                    if (interfaces.Length == 1)
                    {
                        var serviceType = interfaces.Single();
                        if (serviceType.FullName == $"{concreteClass.Namespace}.I{concreteClass.Name}")
                            builder.Services.AddScoped(serviceType, concreteClass);
                    }
                    else if (interfaces.Length == 0)
                    {
                        if (!concreteClass.Name.EndsWith("Command")
                            && !concreteClass.Name.EndsWith("Query")
                            && !concreteClass.Name.EndsWith("Service"))
                        {
                            continue;
                        }
                        builder.Services.AddScoped(concreteClass);
                    }
                }
            }
        }
    }
}
