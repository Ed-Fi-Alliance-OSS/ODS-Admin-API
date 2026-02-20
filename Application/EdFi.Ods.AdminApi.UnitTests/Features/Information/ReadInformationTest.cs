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
        A.CallTo(() => tenantsService.GetTenantsAsync(A<bool>._)).Returns(
        [
            new TenantModel { TenantName = "tenant1" },
            new TenantModel { TenantName = "tenant2" }
        ]);

        var result = await ReadInformation.GetInformation(options, tenantsService);

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
        var tenantsService = A.Fake<ITenantsService>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V2", MultiTenancy = false });

        var result = await ReadInformation.GetInformation(options, tenantsService);

        result.ShouldNotBeNull();
        result.Tenancy.ShouldNotBeNull();
        result.Tenancy.MultitenantMode.ShouldBeFalse();
        result.Tenancy.Tenants.ShouldBeEmpty();
        A.CallTo(() => tenantsService.GetTenantsAsync(A<bool>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task GetInformation_V2Mode_ReturnsVersionAndBuild()
    {
        var options = A.Fake<IOptions<AppSettings>>();
        var tenantsService = A.Fake<ITenantsService>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V2", MultiTenancy = false });

        var result = await ReadInformation.GetInformation(options, tenantsService);

        result.Version.ShouldBe(EdFi.Ods.AdminApi.Infrastructure.Helpers.ConstantsHelpers.Version);
        result.Build.ShouldBe(EdFi.Ods.AdminApi.Infrastructure.Helpers.ConstantsHelpers.Build);
    }

    [Test]
    public async Task GetInformation_V1Mode_ReturnsVersionAndBuild()
    {
        var options = A.Fake<IOptions<AppSettings>>();
        var tenantsService = A.Fake<ITenantsService>();

        A.CallTo(() => options.Value).Returns(new AppSettings { AdminApiMode = "V1", MultiTenancy = false });

        var result = await ReadInformation.GetInformation(options, tenantsService);

        result.Version.ShouldBe(EdFi.Ods.AdminApi.V1.Infrastructure.Helpers.ConstantsHelpers.Version);
        result.Build.ShouldBe(EdFi.Ods.AdminApi.V1.Infrastructure.Helpers.ConstantsHelpers.Build);
    }
}
