// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Information;
using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Information;

[TestFixture]
public class ReadInformationTests
{
    [Test]
    public void GetInformation_V2Mode_SingleTenant_ReturnsCorrectInformation()
    {
        var appSettings = new AppSettings { AdminApiMode = "v2", MultiTenancy = false };
        var options = A.Fake<IOptions<AppSettings>>();
        A.CallTo(() => options.Value).Returns(appSettings);

        var result = ReadInformation.GetInformation(options);

        result.ShouldNotBeNull();
        result.Version.ShouldNotBeNullOrEmpty();
        result.Build.ShouldNotBeNullOrEmpty();
        result.TenantMode.ShouldBe("singletenant");
    }

    [Test]
    public void GetInformation_V2Mode_MultiTenant_ReturnsCorrectInformation()
    {
        var appSettings = new AppSettings { AdminApiMode = "v2", MultiTenancy = true };
        var options = A.Fake<IOptions<AppSettings>>();
        A.CallTo(() => options.Value).Returns(appSettings);

        var result = ReadInformation.GetInformation(options);

        result.ShouldNotBeNull();
        result.Version.ShouldNotBeNullOrEmpty();
        result.Build.ShouldNotBeNullOrEmpty();
        result.TenantMode.ShouldBe("multitenant");
    }

    [Test]
    public void GetInformation_V1Mode_SingleTenant_ReturnsCorrectInformation()
    {
        var appSettings = new AppSettings { AdminApiMode = "v1", MultiTenancy = false };
        var options = A.Fake<IOptions<AppSettings>>();
        A.CallTo(() => options.Value).Returns(appSettings);

        var result = ReadInformation.GetInformation(options);

        result.ShouldNotBeNull();
        result.Version.ShouldNotBeNullOrEmpty();
        result.Build.ShouldNotBeNullOrEmpty();
        result.TenantMode.ShouldBe("singletenant");
    }

    [Test]
    public void GetInformation_V1Mode_MultiTenant_ReturnsCorrectInformation()
    {
        var appSettings = new AppSettings { AdminApiMode = "v1", MultiTenancy = true };
        var options = A.Fake<IOptions<AppSettings>>();
        A.CallTo(() => options.Value).Returns(appSettings);

        var result = ReadInformation.GetInformation(options);

        result.ShouldNotBeNull();
        result.Version.ShouldNotBeNullOrEmpty();
        result.Build.ShouldNotBeNullOrEmpty();
        result.TenantMode.ShouldBe("multitenant");
    }
}
