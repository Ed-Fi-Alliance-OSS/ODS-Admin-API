// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Reflection;
using EdFi.Ods.AdminApi.AdminConsole.Helpers;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.AutoMapper;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Tenants;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Filters;

namespace EdFi.Ods.AdminApi.AdminConsole;

public static class ServicesBuilderExtension
{
    public static void AddAdminConsoleServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<TenantBackgroundService>();

        builder.Services.Configure<AppSettingsFile>(builder.Configuration);

        builder.Services.Configure<AdminConsoleSettings>(builder.Configuration.GetSection("AdminConsoleSettings"));
        builder.Services.AddAutoMapper(typeof(AdminConsoleMappingProfile));

        builder.Services.AddTransient<IEncryptionKeySettings>(sp => sp.GetService<IOptions<AdminConsoleSettings>>()!.Value);
        builder.Services.AddTransient<IEncryptionKeyResolver, OptionsEncryptionKeyResolver>();

        builder.Services.AddTransient<IAdminConsoleTenantsService, TenantService>();
        builder.Services.AddTransient<IAdminConsoleInstancesService, InstanceService>();
        builder.Services.AddTransient<IAdminConsoleInitializationService, InitializationService>();
        builder.Services.AddTransient<IGetOdsInstancesQuery, GetOdsInstancesQuery>();
        builder.Services.AddTransient<IGetOdsInstanceContextsQuery, GetOdsInstanceContextsQuery>();
        builder.Services.AddTransient<IGetOdsInstanceDerivativesQuery, GetOdsInstanceDerivativesQuery>();
        builder.Services.AddTransient<IGetApiClientIdByApplicationIdQuery, GetApiClientIdByApplicationIdQuery>();
        builder.Services.AddTransient<IGetApplicationByNameAndClaimsetQuery, GetApplicationByNameAndClaimsetQuery>();

        builder.RegisterAdminConsoleServices();
        builder.RegisterAdminConsoleValidators();

    }

    private static void RegisterAdminConsoleServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped(typeof(ICommandRepository<>), typeof(CommandRepository<>));
        builder.Services.AddScoped(typeof(IQueriesRepository<>), typeof(QueriesRepository<>));
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

    private static void RegisterAdminConsoleValidators(this WebApplicationBuilder webApplicationBuilder)
    {
        // Fluent validation
        webApplicationBuilder.Services
            .AddValidatorsFromAssembly(typeof(IMarkerForEdFiAdminConsoleManagement).Assembly)
            .AddFluentValidationAutoValidation();
        ValidatorOptions.Global.DisplayNameResolver = (type, memberInfo, expression)
                    => memberInfo?
                        .GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>()?.GetName();
    }
}
