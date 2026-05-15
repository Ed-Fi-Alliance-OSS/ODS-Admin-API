// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Information;
using EdFi.Ods.AdminApi.Features.Tenants;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
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
    public async Task GetInformation_MultiTenantMode_ReturnsTenantNames()
    {
        var options = A.Fake<IOptions<AppSettings>>();
        var tenantsService = A.Fake<ITenantsService>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V2", MultiTenancy = true });
        A.CallTo(() => tenantsService.GetTenantsAsync(A<bool>._)).ReturnsLazily(call => Task.FromResult(new List<TenantModel>
        {
            new TenantModel { TenantName = "tenant1" },
            new TenantModel { TenantName = "tenant2" }
        }));

        var serviceProvider = new ServiceCollection()
            .AddSingleton(tenantsService)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var result = await ReadInformation.GetInformation(options, httpContext);

        result.ShouldNotBeNull();
        result.Tenancy.ShouldNotBeNull();
        result.Tenancy.MultitenantMode.ShouldBeTrue();
        result.Tenancy.Tenants.Count.ShouldBe(2);
        result.Tenancy.Tenants.ShouldContain("tenant1");
        result.Tenancy.Tenants.ShouldContain("tenant2");
    }

    [Test]
    public async Task GetInformation_SingleTenantMode_ReturnsEmptyTenants()
    {
        var options = A.Fake<IOptions<AppSettings>>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V2", MultiTenancy = false });

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };

        var result = await ReadInformation.GetInformation(options, httpContext);

        result.ShouldNotBeNull();
        result.Tenancy.ShouldNotBeNull();
        result.Tenancy.MultitenantMode.ShouldBeFalse();
        result.Tenancy.Tenants.ShouldBeEmpty();
    }

    [Test]
    public async Task GetInformation_V2Mode_ReturnsVersionAndBuild()
    {
        var options = A.Fake<IOptions<AppSettings>>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V2", MultiTenancy = false });

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };

        var result = await ReadInformation.GetInformation(options, httpContext);

        result.Version.ShouldBe(EdFi.Ods.AdminApi.Infrastructure.Helpers.ConstantsHelpers.Version);
        result.Build.ShouldBe(EdFi.Ods.AdminApi.Infrastructure.Helpers.ConstantsHelpers.Build);
        result.SpecificationVersion.ShouldBe("v2");
    }

    [Test]
    public async Task GetInformation_V1Mode_ReturnsVersionAndBuild()
    {
        var options = A.Fake<IOptions<AppSettings>>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V1", MultiTenancy = false });

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };

        var result = await ReadInformation.GetInformation(options, httpContext);

        result.Version.ShouldBe(EdFi.Ods.AdminApi.V1.Infrastructure.Helpers.ConstantsHelpers.Version);
        result.Build.ShouldBe(EdFi.Ods.AdminApi.V1.Infrastructure.Helpers.ConstantsHelpers.Build);
        result.SpecificationVersion.ShouldBe("v1");
    }

    [Test]
    public async Task GetInformation_V3Mode_ReturnsVersionAndBuild()
    {
        var options = A.Fake<IOptions<AppSettings>>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V3", MultiTenancy = false });

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };

        var result = await ReadInformation.GetInformation(options, httpContext);

        result.Version.ShouldBe(EdFi.Ods.AdminApi.V3.Infrastructure.Helpers.ConstantsHelpers.Version);
        result.Build.ShouldBe(EdFi.Ods.AdminApi.V3.Infrastructure.Helpers.ConstantsHelpers.Build);
        result.SpecificationVersion.ShouldBe("v3");
    }

    [Test]
    public async Task GetInformation_V3MultiTenantMode_ReturnsTenantNames()
    {
        var options = A.Fake<IOptions<AppSettings>>();
        var tenantsService = A.Fake<EdFi.Ods.AdminApi.V3.Infrastructure.Services.Tenants.ITenantsService>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V3", MultiTenancy = true });
        A.CallTo(() => tenantsService.GetTenantsAsync(A<bool>._)).ReturnsLazily(call => Task.FromResult(new List<EdFi.Ods.AdminApi.V3.Features.Tenants.TenantModel>
        {
            new EdFi.Ods.AdminApi.V3.Features.Tenants.TenantModel { TenantName = "tenant1" },
            new EdFi.Ods.AdminApi.V3.Features.Tenants.TenantModel { TenantName = "tenant2" }
        }));

        var serviceProvider = new ServiceCollection()
            .AddSingleton(tenantsService)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        var result = await ReadInformation.GetInformation(options, httpContext);

        result.ShouldNotBeNull();
        result.Tenancy.ShouldNotBeNull();
        result.Tenancy.MultitenantMode.ShouldBeTrue();
        result.Tenancy.Tenants.Count.ShouldBe(2);
        result.Tenancy.Tenants.ShouldContain("tenant1");
        result.Tenancy.Tenants.ShouldContain("tenant2");
    }
}
