// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Reflection;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Common.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Infrastructure.Security;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Api;
using EdFi.Ods.AdminApi.Infrastructure.Documentation;
using EdFi.Ods.AdminApi.Infrastructure.Security;
using EdFi.Ods.AdminApi.Infrastructure.Services;
using EdFi.Security.DataAccess.Contexts;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;

namespace EdFi.Ods.AdminApi.Infrastructure;

public static class WebApplicationBuilderExtensions
{
    private static readonly string[] _value = ["api"];

    public static void AddServices(this WebApplicationBuilder webApplicationBuilder)
    {
        ConfigurationManager config = webApplicationBuilder.Configuration;
        webApplicationBuilder.Services.Configure<AppSettings>(config.GetSection("AppSettings"));
        EnableMultiTenancySupport(webApplicationBuilder);
        var executingAssembly = Assembly.GetExecutingAssembly();
        webApplicationBuilder.Services.AddAutoMapper(
            executingAssembly,
            typeof(AdminApiMappingProfile).Assembly
        );
        webApplicationBuilder.Services.AddScoped<InstanceContext>();

        foreach (var type in typeof(IMarkerForEdFiOdsAdminApiManagement).Assembly.GetTypes())
        {
            if (type.IsClass && !type.IsAbstract && (type.IsPublic || type.IsNestedPublic))
            {
                var concreteClass = type;

                var interfaces = concreteClass.GetInterfaces().ToArray();

                if (concreteClass.Namespace != null)
                {
                    if (
                        !concreteClass.Namespace.EndsWith("Database.Commands")
                        && !concreteClass.Namespace.EndsWith("Database.Queries")
                        && !concreteClass.Namespace.EndsWith("ClaimSetEditor")
                    )
                    {
                        continue;
                    }

                    if (interfaces.Length == 1)
                    {
                        var serviceType = interfaces.Single();
                        if (serviceType.FullName == $"{concreteClass.Namespace}.I{concreteClass.Name}")
                            webApplicationBuilder.Services.AddTransient(serviceType, concreteClass);
                    }
                    else if (interfaces.Length == 0)
                    {
                        if (
                            !concreteClass.Name.EndsWith("Command")
                            && !concreteClass.Name.EndsWith("Query")
                            && !concreteClass.Name.EndsWith("Service")
                        )
                        {
                            continue;
                        }
                        webApplicationBuilder.Services.AddTransient(concreteClass);
                    }
                }
            }
        }

        // Add services to the container.
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        webApplicationBuilder.Services.AddEndpointsApiExplorer();
        webApplicationBuilder.Services.AddApiVersioning(opt =>
        {
            opt.ReportApiVersions = true;
            opt.AssumeDefaultVersionWhenUnspecified = false;
        });

        webApplicationBuilder.Services.Configure<SwaggerSettings>(config.GetSection("SwaggerSettings"));
        var issuer = webApplicationBuilder.Configuration.GetValue<string>("Authentication:IssuerUrl");
        webApplicationBuilder.Services.AddSwaggerGen(opt =>
        {
            opt.CustomSchemaIds(x =>
            {
                var name = x.FullName?.Replace(x.Namespace + ".", "");
                if (name != null && name.Any(c => c == '+'))
                    name = name.Split('+')[1];
                return name.ToCamelCase();
            });
            opt.OperationFilter<TokenEndpointBodyDescriptionFilter>();
            opt.OperationFilter<TagByResourceUrlFilter>();
            opt.AddSecurityDefinition(
                "oauth",
                new OpenApiSecurityScheme
                {
                    Flows = new OpenApiOAuthFlows
                    {
                        ClientCredentials = new OpenApiOAuthFlow
                        {
                            TokenUrl = new Uri($"{issuer}/{SecurityConstants.TokenEndpoint}"),
                            Scopes = SecurityConstants.Scopes.AllScopes.ToDictionary(
                                x => x.Scope,
                                x => x.ScopeDescription
                            ),
                        },
                    },
                    In = ParameterLocation.Header,
                    Name = HeaderNames.Authorization,
                    Type = SecuritySchemeType.OAuth2
                }
            );
            opt.AddSecurityRequirement(
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "oauth"
                            },
                        },
                        _value
                    }
                }
            );

            foreach (var version in AdminApiVersions.GetAllVersionStrings())
            {
                opt.SwaggerDoc(
                    version,
                    new OpenApiInfo
                    {
                        Title = "Admin API Documentation",
                        Description =
                            "The Ed-Fi Admin API is a REST API-based administrative interface for managing vendors, applications, client credentials, and authorization rules for accessing an Ed-Fi API.",
                        Version = version
                    }
                );
            }
            opt.DocumentFilter<ListExplicitSchemaDocumentFilter>();
            opt.SchemaFilter<SwaggerOptionalSchemaFilter>();
            opt.SchemaFilter<SwaggerSchemaRemoveRequiredFilter>();
            opt.SchemaFilter<SwaggerExcludeSchemaFilter>();
            opt.OperationFilter<SwaggerDefaultParameterFilter>();
            opt.OperationFilter<ProfileRequestExampleFilter>();
            opt.EnableAnnotations();
            opt.OrderActionsBy(x =>
            {
                return x.HttpMethod != null && Enum.TryParse<HttpVerbOrder>(x.HttpMethod, out var verb)
                    ? ((int)verb).ToString()
                    : int.MaxValue.ToString();
            });
        });

        // Logging
        var loggingOptions = config.GetSection("Log4NetCore").Get<Log4NetProviderOptions>();
        webApplicationBuilder.Logging.AddLog4Net(loggingOptions);

        // Fluent validation
        webApplicationBuilder
            .Services.AddValidatorsFromAssembly(executingAssembly)
            .AddFluentValidationAutoValidation();
        ValidatorOptions.Global.DisplayNameResolver = (type, memberInfo, expression) =>
            memberInfo
                ?.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>()
                ?.GetName();

        //Databases
        var databaseEngine = config.Get("AppSettings:DatabaseEngine", "SqlServer");
        webApplicationBuilder.AddDatabases(databaseEngine);

        //Health
        webApplicationBuilder.Services.AddHealthCheck(webApplicationBuilder.Configuration);

        //JSON
        webApplicationBuilder.Services.Configure<JsonOptions>(o =>
        {
            o.SerializerOptions.WriteIndented = true;
        });

        webApplicationBuilder.Services.AddSecurityUsingOpenIddict(
            webApplicationBuilder.Configuration,
            webApplicationBuilder.Environment
        );

        webApplicationBuilder.Services.AddHttpClient();
        webApplicationBuilder.Services.AddTransient<ISimpleGetRequest, SimpleGetRequest>();
        webApplicationBuilder.Services.AddTransient<IOdsApiValidator, OdsApiValidator>();
    }

    private static void EnableMultiTenancySupport(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Services.AddTransient<
            ITenantConfigurationProvider,
            TenantConfigurationProvider
        >();
        webApplicationBuilder.Services.AddTransient<
            IContextProvider<TenantConfiguration>,
            ContextProvider<TenantConfiguration>
        >();
        webApplicationBuilder.Services.AddSingleton<IContextStorage, HashtableContextStorage>();
        webApplicationBuilder.Services.AddScoped<TenantResolverMiddleware>();
        webApplicationBuilder.Services.Configure<TenantsSection>(webApplicationBuilder.Configuration);
    }

    private static void AddDatabases(this WebApplicationBuilder webApplicationBuilder, string databaseEngine)
    {
        IConfiguration config = webApplicationBuilder.Configuration;

        var multiTenancyEnabled = config.Get("AppSettings:MultiTenancy", false);

        if (DatabaseEngineEnum.Parse(databaseEngine).Equals(DatabaseEngineEnum.PostgreSql))
        {
            webApplicationBuilder.Services.AddDbContext<AdminApiDbContext>(
                (sp, options) =>
                {
                    options.UseNpgsql(
                        AdminConnectionString(sp),
                        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                    );
                    options.UseLowerCaseNamingConvention();
                    options.UseOpenIddict<ApiApplication, ApiAuthorization, ApiScope, ApiToken, int>();
                }
            );

            webApplicationBuilder.Services.AddScoped<ISecurityContext>(sp => new PostgresSecurityContext(
                SecurityDbContextOptions(sp, DatabaseEngineEnum.PostgreSql)
            ));

            webApplicationBuilder.Services.AddScoped<IUsersContext>(
                sp => new AdminConsolePostgresUsersContext(
                    AdminDbContextOptions(sp, DatabaseEngineEnum.PostgreSql)
                )
            );
        }
        else if (DatabaseEngineEnum.Parse(databaseEngine).Equals(DatabaseEngineEnum.SqlServer))
        {
            webApplicationBuilder.Services.AddDbContext<AdminApiDbContext>(
                (sp, options) =>
                {
                    options.UseSqlServer(
                        AdminConnectionString(sp),
                        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                    );
                    options.UseOpenIddict<ApiApplication, ApiAuthorization, ApiScope, ApiToken, int>();
                }
            );

            webApplicationBuilder.Services.AddScoped<ISecurityContext>(
                (sp) =>
                    new SqlServerSecurityContext(SecurityDbContextOptions(sp, DatabaseEngineEnum.SqlServer))
            );

            webApplicationBuilder.Services.AddScoped<IUsersContext>(
                (sp) =>
                    new AdminConsoleSqlServerUsersContext(
                        AdminDbContextOptions(sp, DatabaseEngineEnum.SqlServer)
                    )
            );
        }
        else
        {
            throw new ArgumentException(
                $"Unexpected DB setup error. Engine '{databaseEngine}' was parsed as valid but is not configured for startup."
            );
        }

        string AdminConnectionString(IServiceProvider serviceProvider)
        {
            var adminConnectionString = string.Empty;

            if (multiTenancyEnabled)
            {
                var tenant = serviceProvider
                    .GetRequiredService<IContextProvider<TenantConfiguration>>()
                    .Get();
                if (tenant != null && !string.IsNullOrEmpty(tenant.AdminConnectionString))
                {
                    adminConnectionString = tenant.AdminConnectionString;
                }
                else
                {
                    throw new ArgumentException(
                        $"Admin database connection setup error. Tenant not configured correctly."
                    );
                }
            }
            else
            {
                adminConnectionString = config.GetConnectionStringByName("EdFi_Admin");
            }

            return adminConnectionString;
        }

        DbContextOptions AdminDbContextOptions(IServiceProvider serviceProvider, string databaseEngine)
        {
            var adminConnectionString = AdminConnectionString(serviceProvider);
            var builder = new DbContextOptionsBuilder();
            if (DatabaseEngineEnum.Parse(databaseEngine).Equals(DatabaseEngineEnum.PostgreSql))
            {
                builder.UseNpgsql(adminConnectionString);
                builder.UseLowerCaseNamingConvention();
            }
            else if (DatabaseEngineEnum.Parse(databaseEngine).Equals(DatabaseEngineEnum.SqlServer))
            {
                builder.UseSqlServer(adminConnectionString);
            }
            return builder.Options;
        }

        string SecurityConnectionString(IServiceProvider serviceProvider)
        {
            var securityConnectionString = string.Empty;

            if (multiTenancyEnabled)
            {
                var tenant = serviceProvider
                    .GetRequiredService<IContextProvider<TenantConfiguration>>()
                    .Get();
                if (tenant != null && !string.IsNullOrEmpty(tenant.SecurityConnectionString))
                {
                    securityConnectionString = tenant.SecurityConnectionString;
                }
                else
                {
                    throw new ArgumentException(
                        $"Security database connection setup error. Tenant not configured correctly."
                    );
                }
            }
            else
            {
                securityConnectionString = config.GetConnectionStringByName("EdFi_Security");
            }

            return securityConnectionString;
        }

        DbContextOptions SecurityDbContextOptions(IServiceProvider serviceProvider, string databaseEngine)
        {
            var securityConnectionString = SecurityConnectionString(serviceProvider);
            var builder = new DbContextOptionsBuilder();
            if (DatabaseEngineEnum.Parse(databaseEngine).Equals(DatabaseEngineEnum.PostgreSql))
            {
                builder.UseNpgsql(securityConnectionString);
                builder.UseLowerCaseNamingConvention();
            }
            else if (DatabaseEngineEnum.Parse(databaseEngine).Equals(DatabaseEngineEnum.SqlServer))
            {
                builder.UseSqlServer(securityConnectionString);
            }

            return builder.Options;
        }
    }

    private enum HttpVerbOrder
    {
        GET = 1,
        POST = 2,
        PUT = 3,
        DELETE = 4,
    }
}
