// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Information;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
    }

    [Test]
    public async Task GetInformation_V3ModeBehindReverseProxy_UsesForwardedProtoAndHost()
    {
        var options = A.Fake<IOptions<AppSettings>>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V3" });

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("adminapi");
        httpContext.Request.PathBase = "/adminapi";
        httpContext.Request.Headers["X-Forwarded-Proto"] = "https";
        httpContext.Request.Headers["X-Forwarded-Host"] = "localhost";

        var result = await ReadInformation.GetInformation(options, httpContext);

        result.Urls.ShouldNotBeNull();
        result.Urls.OpenApiMetadata.ShouldBe($"https://localhost/adminapi/swagger/{AdminApiVersions.V3}/swagger.json");
    }
}
