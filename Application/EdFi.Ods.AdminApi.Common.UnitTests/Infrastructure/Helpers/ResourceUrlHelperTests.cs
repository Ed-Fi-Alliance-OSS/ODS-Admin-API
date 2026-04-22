// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.Helpers;

[TestFixture]
public class ResourceUrlHelperTests
{
    [Test]
    public void BuildAbsoluteResourceUrl_WithVersionedMode_BuildsAbsoluteUrlWithLowercaseVersion()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost", 7214);
        httpContext.Request.PathBase = "/";

        var result = ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V3, "/vendors/993");

        result.ShouldBe("https://localhost:7214/v3/vendors/993");
    }

    [Test]
    public void BuildAbsoluteResourceUrl_WithoutLeadingSlash_NormalizesResourcePath()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("api.ed-fi.org");

        var result = ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V2, "apiclients/101");

        result.ShouldBe("https://api.ed-fi.org/v2/apiclients/101");
    }

    [Test]
    public void BuildAbsoluteResourceUrl_WithPathBase_IncludesPathBase()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost", 5001);
        httpContext.Request.PathBase = "/adminapi";

        var result = ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V1, "/vendors/1");

        result.ShouldBe("https://localhost:5001/adminapi/v1/vendors/1");
    }

    [Test]
    public void BuildAbsoluteResourceUrl_WithNullHttpContext_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            ResourceUrlHelper.BuildAbsoluteResourceUrl(null!, AdminApiMode.V3, "/vendors/1"));
    }

    [Test]
    public void BuildAbsoluteResourceUrl_WithUnversionedMode_ThrowsArgumentException()
    {
        var httpContext = new DefaultHttpContext();

        var ex = Should.Throw<ArgumentException>(() =>
            ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.Unversioned, "/vendors/1"));

        ex.ParamName.ShouldBe("apiMode");
    }

    [Test]
    public void BuildAbsoluteResourceUrl_WithEmptyResourcePath_ThrowsArgumentException()
    {
        var httpContext = new DefaultHttpContext();

        var ex = Should.Throw<ArgumentException>(() =>
            ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V3, string.Empty));

        ex.ParamName.ShouldBe("relativeResourcePath");
    }
}
