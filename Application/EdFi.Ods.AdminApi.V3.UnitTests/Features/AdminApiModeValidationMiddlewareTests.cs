// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features;

[TestFixture]
public class AdminApiModeValidationMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_WhenVersionMismatch_ReturnsProblemDetails()
    {
        var middleware = new AdminApiModeValidationMiddleware(
            _ => Task.CompletedTask,
            Options.Create(new AppSettings { AdminApiMode = "v2" })
        );

        var context = new DefaultHttpContext();
        context.Request.Path = "/v3/vendors";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        context.Response.ContentType.ShouldContain("application/problem+json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
        responseBody.ShouldContain("Wrong API version for this instance mode.");

        using var doc = JsonDocument.Parse(responseBody);
        doc.RootElement.GetProperty("detail").GetString().ShouldBe("Wrong API version for this instance mode.");
    }
}
