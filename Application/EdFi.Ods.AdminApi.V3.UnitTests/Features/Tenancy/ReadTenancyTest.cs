// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Features.Tenants;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features.Tenancy;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Tenants;
using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.Tenancy;

[TestFixture]
public class ReadTenancyTest
{
    [Test]
    public async Task GetTenancyAsync_MultiTenantMode_ReturnsTenantNames()
    {
        var options = A.Fake<IOptions<AppSettings>>();
        var tenantsService = A.Fake<ITenantsService>();

        A.CallTo(() => options.Value).Returns(new AppSettings { MultiTenancy = true });
        A.CallTo(() => tenantsService.GetTenantsAsync(A<bool>._)).ReturnsLazily(call => Task.FromResult(new List<TenantModel>
        {
            new TenantModel { TenantName = "tenant1" },
            new TenantModel { TenantName = "tenant2" }
        }));

        var result = await ReadTenancy.GetTenancyAsync(tenantsService, options);

        result.Tenants.Count.ShouldBe(2);
        result.Tenants.ShouldContain("tenant1");
        result.Tenants.ShouldContain("tenant2");
    }

    [Test]
    public async Task GetTenancyAsync_SingleTenantMode_ReturnsEmptyTenants()
    {
        var options = A.Fake<IOptions<AppSettings>>();
        var tenantsService = A.Fake<ITenantsService>();

        A.CallTo(() => options.Value).Returns(new AppSettings { MultiTenancy = false });

        var result = await ReadTenancy.GetTenancyAsync(tenantsService, options);

        result.Tenants.ShouldBeEmpty();
    }

    [Test]
    public async Task GetTenancyAsync_MultiTenantModeWithNoTenantsConfigured_ThrowsAdminApiException()
    {
        var options = A.Fake<IOptions<AppSettings>>();
        var tenantsService = A.Fake<ITenantsService>();

        A.CallTo(() => options.Value).Returns(new AppSettings { MultiTenancy = true });
        A.CallTo(() => tenantsService.GetTenantsAsync(A<bool>._)).ReturnsLazily(call => Task.FromResult(new List<TenantModel>()));

        var exception = await Should.ThrowAsync<AdminApiException>(() => ReadTenancy.GetTenancyAsync(tenantsService, options));

        exception.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        exception.Message.ShouldBe("MultiTenancy is enabled but no tenants are configured. Check the Tenants section of appsettings.");
    }
}
