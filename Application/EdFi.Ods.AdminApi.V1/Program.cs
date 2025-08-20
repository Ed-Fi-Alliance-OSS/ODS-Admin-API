// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Features;
using EdFi.Ods.AdminApi.V1.Infrastructure;
using log4net;

var builder = WebApplication.CreateBuilder(args);

builder.AddServices();

// logging
var _logger = LogManager.GetLogger("Program");
_logger.Info("Starting Admin API");

var app = builder.Build();

var pathBase = app.Configuration.GetValue<string>("AppSettings:PathBase");
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase("/" + pathBase.Trim('/'));
    app.UseForwardedHeaders();
}

AdminApiVersions.Initialize(app);

//The ordering here is meaningful: Logging -> Routing -> Auth -> Endpoints
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapFeatureEndpoints();
app.MapControllers();
app.UseHealthChecks("/health");

if (app.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwagger();
    app.DefineSwaggerUIWithApiVersions(AdminApiVersions.GetAllVersionStrings());
}

app.Run();
