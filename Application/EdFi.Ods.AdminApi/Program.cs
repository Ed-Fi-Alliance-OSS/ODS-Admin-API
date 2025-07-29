// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole;
using EdFi.Ods.AdminApi.AdminConsole.Configurations;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Features;
using EdFi.Ods.AdminApi.Infrastructure;
using log4net;

var builder = WebApplication.CreateBuilder(args);

// logging
var _logger = LogManager.GetLogger("Program");
_logger.Info("Starting Admin API");
var adminConsoleIsEnabled = builder.Configuration.GetValue<bool>("AppSettings:EnableAdminConsoleAPI");

//Order is important to enable CORS
if (adminConsoleIsEnabled)
    builder.RegisterAdminConsoleCorsDependencies(_logger);

builder.AddServices();

if (adminConsoleIsEnabled)
    builder.RegisterAdminConsoleDependencies();

var app = builder.Build();

//Order is important to enable CORS
if (adminConsoleIsEnabled)
    app.UseCorsForAdminConsole();

var pathBase = app.Configuration.GetValue<string>("AppSettings:PathBase");
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase($"/{pathBase.Trim('/')}");
    app.UseForwardedHeaders();
}

AdminApiVersions.Initialize(app);

//The ordering here is meaningful: Logging -> Routing -> Auth -> Endpoints
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<TenantResolverMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapFeatureEndpoints();

//Map AdminConsole endpoints if the flag is enable
if (adminConsoleIsEnabled)
{
    app.MapAdminConsoleFeatureEndpoints();
    //Initialize data
    await app.InitAdminConsoleData();
}

app.MapControllers();
app.UseHealthChecks("/health");

if (app.Configuration.GetValue<bool>("SwaggerSettings:EnableSwagger"))
{
    app.UseSwagger();
    app.DefineSwaggerUIWithApiVersions(AdminApiVersions.GetAllVersionStrings());
}

await app.RunAsync();


