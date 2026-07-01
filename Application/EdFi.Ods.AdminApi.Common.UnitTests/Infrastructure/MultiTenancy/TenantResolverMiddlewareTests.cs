// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using FakeItEasy;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.MultiTenancy;

[TestFixture]
public class TenantResolverMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_WhenV2MultiTenantRequestHasValidTenantHeader_SetsTenantConfigurationAndCallsNext()
    {
        var tenantConfiguration = new TenantConfiguration { TenantIdentifier = "tenant-1" };
        var contextProvider = A.Fake<IContextProvider<TenantConfiguration>>();
        var middleware = CreateMiddleware(
            tenantConfigurations: new Dictionary<string, TenantConfiguration>(StringComparer.OrdinalIgnoreCase)
            {
                ["tenant-1"] = tenantConfiguration
            },
            contextProvider: contextProvider
        );

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/v2/vendors";
        context.Request.Headers["tenant"] = "tenant-1";
        var nextCalled = false;

        await middleware.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;

                return Task.CompletedTask;
            }
        );

        nextCalled.ShouldBeTrue();
        A.CallTo(() => contextProvider.Set(tenantConfiguration)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task InvokeAsync_WhenV3MultiTenantPostRequestHasNoTenantHeader_ThrowsValidationException()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/v3/vendors";

        var exception = await Should.ThrowAsync<ValidationException>(() =>
            middleware.InvokeAsync(context, _ => Task.CompletedTask));

        exception.Errors.Single().PropertyName.ShouldBe("Tenant");
        exception.Errors.Single().ErrorMessage.ShouldBe($"{ErrorMessagesConstants.Tenant_MissingHeader} (adminconsole)");
    }

    [Test]
    public async Task InvokeAsync_WhenSwaggerRequestHasValidDefaultTenant_SetsDefaultTenantConfigurationAndCallsNext()
    {
        var tenantConfiguration = new TenantConfiguration { TenantIdentifier = "default-tenant" };
        var contextProvider = A.Fake<IContextProvider<TenantConfiguration>>();
        var middleware = CreateMiddleware(
            tenantConfigurations: new Dictionary<string, TenantConfiguration>(StringComparer.OrdinalIgnoreCase)
            {
                ["default-tenant"] = tenantConfiguration
            },
            contextProvider: contextProvider,
            swaggerSettings: new SwaggerSettings
            {
                EnableSwagger = true,
                DefaultTenant = "default-tenant"
            }
        );

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/swagger/index.html";
        var nextCalled = false;

        await middleware.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;

                return Task.CompletedTask;
            }
        );

        nextCalled.ShouldBeTrue();
        A.CallTo(() => contextProvider.Set(tenantConfiguration)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task InvokeAsync_WhenV1ModeAndTenantHeaderIsMissing_SkipsTenantValidationAndCallsNext()
    {
        var middleware = CreateMiddleware(appSettings: new AppSettings { AdminApiMode = "v1", MultiTenancy = true });
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/adminconsole/vendors";
        var nextCalled = false;

        await middleware.InvokeAsync(
            context,
            _ =>
            {
                nextCalled = true;

                return Task.CompletedTask;
            }
        );

        nextCalled.ShouldBeTrue();
    }

    [Test]
    public async Task InvokeAsync_WhenTenantHeaderHasInvalidFormat_ThrowsValidationException()
    {
        var middleware = CreateMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/v3/vendors";
        context.Request.Headers["tenant"] = "tenant_1";

        var exception = await Should.ThrowAsync<ValidationException>(() =>
            middleware.InvokeAsync(context, _ => Task.CompletedTask));

        exception.Errors.Single().PropertyName.ShouldBe("Tenant");
        exception.Errors.Single().ErrorMessage.ShouldBe(ErrorMessagesConstants.Tenant_InvalidFormat);
    }

    private static TenantResolverMiddleware CreateMiddleware(
        IDictionary<string, TenantConfiguration>? tenantConfigurations = null,
        IContextProvider<TenantConfiguration>? contextProvider = null,
        AppSettings? appSettings = null,
        SwaggerSettings? swaggerSettings = null
    )
    {
        var tenantConfigurationProvider = A.Fake<ITenantConfigurationProvider>();
        A.CallTo(() => tenantConfigurationProvider.Get()).Returns(
            tenantConfigurations ?? new Dictionary<string, TenantConfiguration>(StringComparer.OrdinalIgnoreCase)
        );

        return new TenantResolverMiddleware(
            tenantConfigurationProvider,
            contextProvider ?? A.Fake<IContextProvider<TenantConfiguration>>(),
            Options.Create(appSettings ?? new AppSettings { AdminApiMode = "v3", MultiTenancy = true }),
            Options.Create(swaggerSettings ?? new SwaggerSettings())
        );
    }
}
