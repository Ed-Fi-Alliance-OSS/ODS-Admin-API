// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using System.Reflection;
using System.Threading.RateLimiting;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Common.Extensions;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Infrastructure.Security;
using EdFi.Ods.AdminApi.Common.Infrastructure.Services;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Connect;
using EdFi.Ods.AdminApi.Infrastructure.Documentation;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Infrastructure.Security;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using EdFi.Security.DataAccess.Contexts;
using FluentValidation;
using FluentValidation.AspNetCore;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;

namespace EdFi.Ods.AdminApi.Infrastructure;

public static class WebApplicationBuilderExtensions
{
    private static readonly string[] _value = ["api"];

    public static void AddServices(this WebApplicationBuilder webApplicationBuilder)
    {
        webApplicationBuilder.Services.AddSingleton<ISymmetricStringEncryptionProvider, Aes256SymmetricStringEncryptionProvider>();

        var env = webApplicationBuilder.Environment;
        var appSettingsPath = Path.Combine(env.ContentRootPath, "appsettings.json");
        webApplicationBuilder.Services.AddSingleton<IAppSettingsFileProvider>(new FileSystemAppSettingsFileProvider(appSettingsPath));

        ConfigureRateLimiting(webApplicationBuilder);
        ConfigurationManager config = webApplicationBuilder.Configuration;
        webApplicationBuilder.Services.Configure<AppSettings>(config.GetSection("AppSettings"));
        EnableMultiTenancySupport(webApplicationBuilder);

        var adminApiMode = config.GetValue<AdminApiMode>("AppSettings:AdminApiMode", AdminApiMode.V2);
        Assembly assembly;

        webApplicationBuilder.Services.AddScoped<InstanceContext>();

        if (adminApiMode == AdminApiMode.V2)
        {
            assembly = Assembly.GetExecutingAssembly();

            webApplicationBuilder.Services.AddAutoMapper(
                assembly,
                typeof(AdminApiMappingProfile).Assembly
            );

            var adminApiV2Types = typeof(IMarkerForEdFiOdsAdminApiManagement).Assembly.GetTypes();
            RegisterAdminApiServices(webApplicationBuilder, adminApiV2Types);
        }
        else
        {
            assembly = Assembly.Load("EdFi.Ods.AdminApi.V1");

            webApplicationBuilder.Services.AddAutoMapper(
                assembly,
                typeof(V1.Infrastructure.AutoMapper.AdminApiMappingProfile).Assembly
            );

            var adminApiV1Types = typeof(V1.Infrastructure.IMarkerForEdFiOdsAdminApiManagement).Assembly.GetTypes();
            RegisterAdminApiServices(webApplicationBuilder, adminApiV1Types);
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
            opt.EnableAnnotations();
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

            opt.OrderActionsBy(x =>
            {
                return x.HttpMethod != null && Enum.TryParse<HttpVerbOrder>(x.HttpMethod, out var verb)
                    ? ((int)verb).ToString()
                    : int.MaxValue.ToString();
            });
        });

        // Fluent validation
        webApplicationBuilder
            .Services.AddValidatorsFromAssembly(assembly)
            .AddFluentValidationAutoValidation();

        webApplicationBuilder.Services.AddTransient<RegisterService.Validator>();

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

        webApplicationBuilder.Services.Configure<AppSettingsFile>(webApplicationBuilder.Configuration);

        webApplicationBuilder.Services.AddTransient<ITenantsService, TenantService>();
    }

    public static void AddLoggingServices(this WebApplicationBuilder webApplicationBuilder)
    {
        ConfigurationManager config = webApplicationBuilder.Configuration;

        // Remove all default logging providers (Console, Debug, etc.)
        webApplicationBuilder.Logging.ClearProviders();

        // Initialize log4net early so we can use it in Program.cs
        var log4netConfigFileName = webApplicationBuilder.Configuration.GetValue<string>("Log4NetCore:Log4NetConfigFileName");
        if (!string.IsNullOrEmpty(log4netConfigFileName))
        {
            var log4netConfigPath = Path.Combine(AppContext.BaseDirectory, log4netConfigFileName);
            if (File.Exists(log4netConfigPath))
            {
                var log4netConfig = new FileInfo(log4netConfigPath);
                XmlConfigurator.Configure(LogManager.GetRepository(), log4netConfig);
            }
        }

        // Important to display messages based on the Logging section in appsettings.json
        var loggingOptions = config.GetSection("Log4NetCore").Get<Log4NetProviderOptions>();
        webApplicationBuilder.Logging.AddLog4Net(loggingOptions);
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

        var adminApiMode = config.GetValue<AdminApiMode>("AppSettings:AdminApiMode", AdminApiMode.V2);
        var multiTenancyEnabled = config.Get("AppSettings:MultiTenancy", false);

        switch (adminApiMode)
        {
            case AdminApiMode.V1:
                if (DatabaseEngineEnum.Parse(databaseEngine).Equals(DatabaseEngineEnum.PostgreSql))
                {
                    var adminConnectionString = webApplicationBuilder.Configuration.GetConnectionString("EdFi_Admin");
                    var securityConnectionString = webApplicationBuilder.Configuration.GetConnectionString("EdFi_Security");

                    webApplicationBuilder.Services.AddDbContext<AdminApiDbContext>(
                        options =>
                        {
                            options.UseNpgsql(adminConnectionString);
                            options.UseOpenIddict<ApiApplication, ApiAuthorization, ApiScope, ApiToken, int>();
                        });

                    var optionsBuilder = new DbContextOptionsBuilder();
                    optionsBuilder.UseNpgsql(securityConnectionString);
                    optionsBuilder.UseLowerCaseNamingConvention();

                    webApplicationBuilder.Services.AddScoped<V1.Security.DataAccess.Contexts.ISecurityContext>(
                        sp => new V1.Security.DataAccess.Contexts.PostgresSecurityContext(SecurityDbContextOptions(sp, DatabaseEngineEnum.PostgreSql)));

                    webApplicationBuilder.Services.AddScoped<V1.Admin.DataAccess.Contexts.IUsersContext>(
                        sp => new V1.Admin.DataAccess.Contexts.PostgresUsersContext(AdminDbContextOptions(sp, DatabaseEngineEnum.PostgreSql)));
                }
                else if (DatabaseEngineEnum.Parse(databaseEngine).Equals(DatabaseEngineEnum.SqlServer))
                {
                    var adminConnectionString = webApplicationBuilder.Configuration.GetConnectionString("EdFi_Admin");
                    var securityConnectionString = webApplicationBuilder.Configuration.GetConnectionString("EdFi_Security");

                    webApplicationBuilder.Services.AddDbContext<AdminApiDbContext>(
                       options =>
                       {
                           options.UseSqlServer(adminConnectionString);
                           options.UseOpenIddict<ApiApplication, ApiAuthorization, ApiScope, ApiToken, int>();
                       });

                    var optionsBuilder = new DbContextOptionsBuilder();
                    optionsBuilder.UseSqlServer(securityConnectionString);

                    webApplicationBuilder.Services.AddScoped<V1.Security.DataAccess.Contexts.ISecurityContext>(
                        sp => new V1.Security.DataAccess.Contexts.SqlServerSecurityContext(SecurityDbContextOptions(sp, DatabaseEngineEnum.SqlServer)));

                    webApplicationBuilder.Services.AddScoped<V1.Admin.DataAccess.Contexts.IUsersContext>(
                        sp => new V1.Admin.DataAccess.Contexts.SqlServerUsersContext(AdminDbContextOptions(sp, DatabaseEngineEnum.SqlServer)));
                }
                else
                {
                    throw new ArgumentException(
                        $"Unexpected DB setup error. Engine '{databaseEngine}' was parsed as valid but is not configured for startup."
                    );
                }
                break;
            case AdminApiMode.V2:
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
                break;
            default:
                throw new InvalidOperationException($"Invalid adminApiMode: {adminApiMode}. Must be 'v1' or 'v2'");
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

    private static void RegisterAdminApiServices(WebApplicationBuilder webApplicationBuilder, Type[] types)
    {
        foreach (var type in types)
        {
            if (type.IsClass && !type.IsAbstract && (type.IsPublic || type.IsNestedPublic))
            {
                var concreteClass = type;

                var interfaces = concreteClass.GetInterfaces().ToArray();

                if (concreteClass.Namespace is not null)
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
                        {
                            webApplicationBuilder.Services.AddTransient(serviceType, concreteClass);
                        }
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
    }

    public static void ConfigureRateLimiting(WebApplicationBuilder builder)
    {
        // Bind IpRateLimiting section
        builder.Services.Configure<IpRateLimitingOptions>(builder.Configuration.GetSection("IpRateLimiting"));

        // Add new rate limiting policy using config
        builder.Services.AddRateLimiter(options =>
        {
            var config = builder.Configuration.GetSection("IpRateLimiting").Get<IpRateLimitingOptions>();

            if (config == null || !config.EnableEndpointRateLimiting)
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ => RateLimitPartition.GetNoLimiter("none"));
                return;
            }
            // Set global options
            options.RejectionStatusCode = config?.HttpStatusCode ?? (int)HttpStatusCode.TooManyRequests;

            if (config?.GeneralRules != null)
            {
                foreach (var rule in config.GeneralRules)
                {
                    // Only support fixed window for now, parse period (e.g., "1m")
                    var window = rule.Period.EndsWith('m') ? TimeSpan.FromMinutes(int.Parse(rule.Period.TrimEnd('m'))) : TimeSpan.FromMinutes(1);
                    // Register a named limiter for each endpoint
                    options.AddFixedWindowLimiter(rule.Endpoint, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rule.Limit,
                        Window = window,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
                }
                // Use a global policy selector to apply endpoint-specific limiters
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var path = context.Request.Path.Value;
                    var method = context.Request.Method;
                    foreach (var rule in config.GeneralRules)
                    {
                        var parts = rule.Endpoint.Split(':');
                        // Only support fixed window for now, parse period (e.g., "1m")
                        var window = rule.Period.EndsWith('m') ? TimeSpan.FromMinutes(int.Parse(rule.Period.TrimEnd('m'))) : TimeSpan.FromMinutes(1);
                        if (path != null && parts.Length == 2 && method.Equals(parts[0], StringComparison.OrdinalIgnoreCase) && path.Equals(parts[1], StringComparison.OrdinalIgnoreCase))
                        {
                            return RateLimitPartition.GetFixedWindowLimiter(rule.Endpoint, _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = rule.Limit,
                                Window = window,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 0
                            });
                        }
                    }
                    // No limiter for this endpoint
                    return RateLimitPartition.GetNoLimiter("none");
                });
            }
        });
    }

    private enum HttpVerbOrder
    {
        GET = 1,
        POST = 2,
        PUT = 3,
        DELETE = 4,
    }
}
