// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features;

[TestFixture]
public class AdminApiModeValidationMiddlewareTests
{
    private static IOptions<AppSettings> Options(string mode) =>
        Microsoft.Extensions.Options.Options.Create(new AppSettings { AdminApiMode = mode });

    [Test]
    public async Task InvokeAsync_WhenVersionMatchesMode_CallsNext()
    {
        var nextCalled = false;
        var middleware = new AdminApiModeValidationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, Options("V2"));
        var context = new DefaultHttpContext();
        context.Request.Path = "/v2/vendors";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        nextCalled.ShouldBeTrue();
    }

    [Test]
    public async Task InvokeAsync_WhenVersionMismatch_ReturnsBadRequest()
    {
        var middleware = new AdminApiModeValidationMiddleware(_ => Task.CompletedTask, Options("V2"));
        var context = new DefaultHttpContext();
        context.Request.Path = "/v3/dataStores";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(400);
    }

    [Test]
    public async Task InvokeAsync_WhenUnversionedPath_CallsNext()
    {
        var nextCalled = false;
        var middleware = new AdminApiModeValidationMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, Options("V2"));
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        nextCalled.ShouldBeTrue();
    }
}
