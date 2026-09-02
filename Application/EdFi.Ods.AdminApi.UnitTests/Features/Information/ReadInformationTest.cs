// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Information;
using FakeItEasy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Information;

[TestFixture]
public class ReadInformationTest
{
    [Test]
    public async Task GetInformation_V2Mode_ReturnsVersionAndBuild()
    {
        var options = A.Fake<IOptions<AppSettings>>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V2" });

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };

        var result = await ReadInformation.GetInformation(options, httpContext);

        result.Version.ShouldBe(ApiInformationHelper.Version);
        result.Build.ShouldBe(ApiInformationHelper.Build);
        result.SpecificationVersion.ShouldBe("v2");
    }

    [Test]
    public async Task GetInformation_V2Mode_ReturnsApplicationNameInformationalVersionAndUrls()
    {
        var options = A.Fake<IOptions<AppSettings>>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V2" });

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("admin-api.example.com");
        httpContext.Request.PathBase = "/v2";

        var result = await ReadInformation.GetInformation(options, httpContext);

        result.ApplicationName.ShouldBe(ApiInformationHelper.ApplicationName);
        result.InformationalVersion.ShouldBe(ApiInformationHelper.InformationalVersion);
        result.Urls.ShouldNotBeNull();
        result.Urls.OpenApiMetadata.ShouldBe($"https://admin-api.example.com/v2/swagger/{AdminApiVersions.V2}/swagger.json");
        result.Urls.Tenancy.ShouldBe($"https://admin-api.example.com/v2/{AdminApiVersions.V2.VersionPath}/tenancy");
    }

    [Test]
    public async Task GetInformation_V1Mode_ReturnsVersionAndBuild()
    {
        var options = A.Fake<IOptions<AppSettings>>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V1" });

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };

        var result = await ReadInformation.GetInformation(options, httpContext);

        result.Version.ShouldBe(ApiInformationHelper.Version);
        result.Build.ShouldBe(ApiInformationHelper.Build);
        result.SpecificationVersion.ShouldBe("v1");
    }

    [Test]
    public async Task GetInformation_V1Mode_ReturnsApplicationNameInformationalVersionAndUrls()
    {
        var options = A.Fake<IOptions<AppSettings>>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V1" });

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("admin-api.example.com");
        httpContext.Request.PathBase = "/v1";

        var result = await ReadInformation.GetInformation(options, httpContext);

        result.ApplicationName.ShouldBe(ApiInformationHelper.ApplicationName);
        result.InformationalVersion.ShouldBe(ApiInformationHelper.InformationalVersion);
        result.Urls.ShouldNotBeNull();
        result.Urls.OpenApiMetadata.ShouldBe($"https://admin-api.example.com/v1/swagger/{AdminApiVersions.V1}/swagger.json");
        result.Urls.Tenancy.ShouldBe(string.Empty);
    }

    [Test]
    public async Task GetInformation_V3Mode_ReturnsVersionAndBuild()
    {
        var options = A.Fake<IOptions<AppSettings>>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V3" });

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };

        var result = await ReadInformation.GetInformation(options, httpContext);

        result.Version.ShouldBe(ApiInformationHelper.Version);
        result.Build.ShouldBe(ApiInformationHelper.Build);
        result.SpecificationVersion.ShouldBe("v3");
    }

    [Test]
    public async Task GetInformation_V3Mode_ReturnsApplicationNameInformationalVersionAndUrls()
    {
        var options = A.Fake<IOptions<AppSettings>>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V3" });

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("admin-api.example.com");
        httpContext.Request.PathBase = "/v3";

        var result = await ReadInformation.GetInformation(options, httpContext);

        result.ApplicationName.ShouldBe(ApiInformationHelper.ApplicationName);
        result.InformationalVersion.ShouldBe(ApiInformationHelper.InformationalVersion);
        result.Urls.ShouldNotBeNull();
        result.Urls.OpenApiMetadata.ShouldBe($"https://admin-api.example.com/v3/swagger/{AdminApiVersions.V3}/swagger.json");
        result.Urls.Tenancy.ShouldBe($"https://admin-api.example.com/v3/{AdminApiVersions.V3.VersionPath}/tenancy");
    }

    [Test]
    public async Task GetInformation_V3ModeBehindReverseProxy_UsesForwardedProtoAndHost()
    {
        // ForwardedHeadersMiddleware (wired via ForwardedHeadersConfigurator when
        // ReverseProxySettings.UseForwardedHeaders is enabled) normalizes Request.Scheme/Request.Host
        // from X-Forwarded-Proto/X-Forwarded-Host *before* ReadInformation runs; ReadInformation itself
        // no longer reads those headers, so this simulates the middleware's effect directly.
        var options = A.Fake<IOptions<AppSettings>>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V3" });

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");
        httpContext.Request.PathBase = "/adminapi";

        var result = await ReadInformation.GetInformation(options, httpContext);

        result.Urls.ShouldNotBeNull();
        result.Urls.OpenApiMetadata.ShouldBe($"https://localhost/adminapi/swagger/{AdminApiVersions.V3}/swagger.json");
        result.Urls.Tenancy.ShouldBe($"https://localhost/adminapi/{AdminApiVersions.V3.VersionPath}/tenancy");
    }

    [Test]
    public async Task GetInformation_V3ModeWithForwardedHeadersMiddlewareEnabled_UsesForwardedProtoAndHost()
    {
        var options = A.Fake<IOptions<AppSettings>>();
        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V3" });

        var reverseProxySettings = new ReverseProxySettings
        {
            UseForwardedHeaders = true,
            KnownProxies = "10.0.0.1"
        };
        var forwardedHeadersOptions = new ForwardedHeadersOptions();
        ForwardedHeadersConfigurator.Configure(forwardedHeadersOptions, reverseProxySettings);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("adminapi");
        httpContext.Request.PathBase = "/adminapi";
        httpContext.Request.Headers["X-Forwarded-Proto"] = "https";
        httpContext.Request.Headers["X-Forwarded-Host"] = "localhost";

        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(forwardedHeadersOptions));

        await middleware.Invoke(httpContext);
        var result = await ReadInformation.GetInformation(options, httpContext);

        result.Urls.ShouldNotBeNull();
        result.Urls.OpenApiMetadata.ShouldBe($"https://localhost/adminapi/swagger/{AdminApiVersions.V3}/swagger.json");
        result.Urls.Tenancy.ShouldBe($"https://localhost/adminapi/{AdminApiVersions.V3.VersionPath}/tenancy");
    }

    [Test]
    public async Task GetInformation_V3ModeWithForwardedHeadersFromUntrustedSource_IgnoresForwardedProtoAndHost()
    {
        var options = A.Fake<IOptions<AppSettings>>();
        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V3" });

        var reverseProxySettings = new ReverseProxySettings
        {
            UseForwardedHeaders = true,
            KnownProxies = "10.0.0.1"
        };
        var forwardedHeadersOptions = new ForwardedHeadersOptions();
        ForwardedHeadersConfigurator.Configure(forwardedHeadersOptions, reverseProxySettings);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.5"); // not in KnownProxies
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("adminapi");
        httpContext.Request.PathBase = "/adminapi";
        httpContext.Request.Headers["X-Forwarded-Proto"] = "https";
        httpContext.Request.Headers["X-Forwarded-Host"] = "spoofed-host";

        var middleware = new ForwardedHeadersMiddleware(
            _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Options.Create(forwardedHeadersOptions));

        await middleware.Invoke(httpContext);
        var result = await ReadInformation.GetInformation(options, httpContext);

        result.Urls.ShouldNotBeNull();
        result.Urls.OpenApiMetadata.ShouldBe($"http://adminapi/adminapi/swagger/{AdminApiVersions.V3}/swagger.json");
        result.Urls.Tenancy.ShouldBe($"http://adminapi/adminapi/{AdminApiVersions.V3.VersionPath}/tenancy");
    }
}
