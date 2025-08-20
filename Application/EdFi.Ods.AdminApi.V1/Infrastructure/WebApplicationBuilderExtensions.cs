// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Reflection;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Infrastructure.Documentation;
using EdFi.Ods.AdminApi.V1.Infrastructure.Security;
using EdFi.Ods.AdminApi.V1.Infrastructure.Api;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Infrastructure.Services;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using FluentValidation;
using EdFi.Ods.AdminApi.V1.Helpers;

namespace EdFi.Ods.AdminApi.V1.Infrastructure;

public static class WebApplicationBuilderExtensions
{
    public static void AddServices(this WebApplicationBuilder webApplicationBuilder)
    {
        IConfiguration config = webApplicationBuilder.Configuration;
        webApplicationBuilder.Services.Configure<AppSettings>(config.GetSection("AppSettings"));

        var executingAssembly = Assembly.GetExecutingAssembly();
        webApplicationBuilder.Services.AddAutoMapper(executingAssembly, typeof(AdminApiMappingProfile).Assembly);
        webApplicationBuilder.Services.AddScoped<InstanceContext>();

        foreach (var type in typeof(IMarkerForEdFiOdsAdminApiManagement).Assembly.GetTypes())
        {
            if (type.IsClass && !type.IsAbstract && (type.IsPublic || type.IsNestedPublic))
            {
                var concreteClass = type;

                var interfaces = concreteClass.GetInterfaces().ToArray();

                if (concreteClass.Namespace != null)
                {
                    if (concreteClass.Namespace.EndsWith("Database.Commands") || concreteClass.Namespace.EndsWith("Database.Queries")
                        || concreteClass.Namespace.EndsWith("ClaimSetEditor"))
                    {
                        if (interfaces.Length == 1)
                        {
                            var serviceType = interfaces.Single();
                            if (serviceType.FullName == $"{concreteClass.Namespace}.I{concreteClass.Name}")
                                webApplicationBuilder.Services.AddTransient(serviceType, concreteClass);
                        }
                        else if (interfaces.Length == 0)
                        {
                            if (concreteClass.Name.EndsWith("Command")
                              || concreteClass.Name.EndsWith("Query")
                              || concreteClass.Name.EndsWith("Service"))
                            {
                                webApplicationBuilder.Services.AddTransient(concreteClass);
                            }
                        }
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

        var issuer = webApplicationBuilder.Configuration.GetValue<string>("Authentication:IssuerUrl");
        webApplicationBuilder.Services.AddSwaggerGen(opt =>
        {
            opt.CustomSchemaIds(x => x.FullName?.Replace("+", "."));
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
                            Scopes = new Dictionary<string, string>
                            {
                                { SecurityConstants.Scopes.AdminApiFullAccess, "Unrestricted access to all Admin API endpoints" },
                            }
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
                                { Type = ReferenceType.SecurityScheme, Id = "oauth" },
                        },
                        new[] { "api" }
                    }
                }
            );

            foreach (var version in AdminApiVersions.GetAllVersionStrings())
            {
                opt.SwaggerDoc(version, new OpenApiInfo
                {
                    Title = "Admin API Documentation", Version = version
                });
            }

            opt.DocumentFilter<ListExplicitSchemaDocumentFilter>();
            opt.SchemaFilter<SwaggerOptionalSchemaFilter>();
            opt.OperationFilter<SwaggerDefaultParameterFilter>();
            opt.EnableAnnotations();
            opt.OrderActionsBy(x =>
            {
                return x.HttpMethod != null && Enum.TryParse<HttpVerbOrder>(x.HttpMethod, out var verb)
                    ? ((int)verb).ToString()
                    : int.MaxValue.ToString();
            });
        });

        // Logging
        var loggingOptions = webApplicationBuilder.Configuration.GetSection("Log4NetCore").Get<Log4NetProviderOptions>();
        webApplicationBuilder.Logging.AddLog4Net(loggingOptions);

        // Fluent validation
        webApplicationBuilder.Services
            .AddValidatorsFromAssembly(executingAssembly)
            .AddFluentValidationAutoValidation();
        ValidatorOptions.Global.DisplayNameResolver = (type, memberInfo, expression)
                    => memberInfo?
                        .GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>()?.GetName();

        //Databases
        var databaseEngine = webApplicationBuilder.Configuration["AppSettings:DatabaseEngine"];
        var (connectionString, isSqlServer) = webApplicationBuilder.AddDatabases(databaseEngine);

        //Health
        webApplicationBuilder.Services.AddHealthCheck(connectionString, isSqlServer);

        //JSON
        webApplicationBuilder.Services.Configure<JsonOptions>(o =>
        {
            o.SerializerOptions.WriteIndented = true;
        });

        webApplicationBuilder.Services.AddSecurityUsingOpenIddict(webApplicationBuilder.Configuration, webApplicationBuilder.Environment);

        webApplicationBuilder.Services.AddHttpClient();
        webApplicationBuilder.Services.AddTransient<ISimpleGetRequest, SimpleGetRequest>();
        webApplicationBuilder.Services.AddTransient<IOdsApiValidator, OdsApiValidator>();


    }

    private static (string adminConnectionString, bool) AddDatabases(this WebApplicationBuilder webApplicationBuilder, string databaseEngine)
    {
        var adminConnectionString = webApplicationBuilder.Configuration.GetConnectionString("Admin");
        var securityConnectionString = webApplicationBuilder.Configuration.GetConnectionString("Security");

        if (DatabaseEngineEnum.Parse(databaseEngine).Equals(DatabaseEngineEnum.PostgreSql))
        {
            webApplicationBuilder.Services.AddDbContext<AdminApiDbContext>(
                options =>
                {
                    options.UseNpgsql(adminConnectionString);
                    options.UseOpenIddict<ApiApplication, ApiAuthorization, ApiScope, ApiToken, int>();
                });

            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseNpgsql(securityConnectionString);
            optionsBuilder.UseLowerCaseNamingConvention();
            var context = new PostgresSecurityContext(optionsBuilder.Options);

            webApplicationBuilder.Services.AddScoped<ISecurityContext>(
                sp => new PostgresSecurityContext(SecurityDbContextOptions(DatabaseEngineEnum.PostgreSql)));

            webApplicationBuilder.Services.AddScoped<IUsersContext>(
                sp => new PostgresUsersContext(AdminDbContextOptions(DatabaseEngineEnum.PostgreSql)));

            return (adminConnectionString, false);
        }

        if (DatabaseEngineEnum.Parse(databaseEngine).Equals(DatabaseEngineEnum.SqlServer))
        {
            webApplicationBuilder.Services.AddDbContext<AdminApiDbContext>(
                options =>
                {
                    options.UseSqlServer(adminConnectionString);
                    options.UseOpenIddict<ApiApplication, ApiAuthorization, ApiScope, ApiToken, int>();
                });

            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlServer(securityConnectionString);
            var context = new SqlServerSecurityContext(optionsBuilder.Options);

            webApplicationBuilder.Services.AddScoped<ISecurityContext>(
                sp => new SqlServerSecurityContext(SecurityDbContextOptions(DatabaseEngineEnum.SqlServer)));

            webApplicationBuilder.Services.AddScoped<IUsersContext>(
                sp => new SqlServerUsersContext(AdminDbContextOptions(DatabaseEngineEnum.SqlServer)));

            return (adminConnectionString, true);
        }

        throw new Exception($"Unexpected DB setup error. Engine '{databaseEngine}' was parsed as valid but is not configured for startup.");

        DbContextOptions AdminDbContextOptions(string databaseEngine)
        {
            DbContextOptionsBuilder builder = new DbContextOptionsBuilder();
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

        DbContextOptions SecurityDbContextOptions(string databaseEngine)
        {
            DbContextOptionsBuilder builder = new DbContextOptionsBuilder();
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
