// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Tenants;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using FakeItEasy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using OdsInstance = EdFi.Admin.DataAccess.Models.OdsInstance;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Services.Tenants;

[TestFixture]
internal class TenantServiceTests
{
    private IOptionsSnapshot<AppSettingsFile> _options = null!;
    private IMemoryCache _memoryCache = null!;
    private AppSettingsFile _appSettings = null!;
    private IGetOdsInstancesQuery _getOdsInstancesQuery = null!;
    private IGetEducationOrganizationQuery _getEducationOrganizationQuery = null!;
    private IMapper _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        _options = A.Fake<IOptionsSnapshot<AppSettingsFile>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mapper = A.Fake<IMapper>();
        _getOdsInstancesQuery = A.Fake<IGetOdsInstancesQuery>();
        _getEducationOrganizationQuery = A.Fake<IGetEducationOrganizationQuery>();
        _appSettings = new AppSettingsFile
        {
            AppSettings = new AppSettings
            {
                MultiTenancy = true,
                DatabaseEngine = "SqlServer"
            },
            Tenants = new Dictionary<string, TenantSettings>
            {
                {
                    "tenantA", new TenantSettings
                    {
                        ConnectionStrings = new Dictionary<string, string>
                        {
                            { "EdFi_Admin", "admin-conn-A" },
                            { "EdFi_Security", "security-conn-A" }
                        }
                    }
                },
                {
                    "tenantB", new TenantSettings
                    {
                        ConnectionStrings = new Dictionary<string, string>
                        {
                            { "EdFi_Admin", "admin-conn-B" },
                            { "EdFi_Security", "security-conn-B" }
                        }
                    }
                }
            },
            ConnectionStrings = new Dictionary<string, string>
            {
                { "EdFi_Admin", "admin-conn-default" },
                { "EdFi_Security", "security-conn-default" }
            },
            SwaggerSettings = new(),
            Testing = new()
        };

        A.CallTo(() => _options.Value).Returns(_appSettings);
    }

    [TearDown]
    public void TearDown()
    {
        _memoryCache.Dispose();
    }

    [Test]
    public async Task GetTenantsAsync_Should_Return_All_Tenants_When_MultiTenancy_Enabled()
    {
        var service = new TenantService(_options, _memoryCache);

        var tenants = await service.GetTenantsAsync();

        tenants.Count.ShouldBe(2);
        tenants.Any(t => t.TenantName == "tenantA").ShouldBeTrue();
        tenants.Any(t => t.TenantName == "tenantB").ShouldBeTrue();
    }

    [Test]
    public async Task GetTenantsAsync_Should_Return_DefaultTenant_When_MultiTenancy_Disabled()
    {
        _appSettings.AppSettings.MultiTenancy = false;
        var service = new TenantService(_options, _memoryCache);

        var tenants = await service.GetTenantsAsync();

        tenants.Count.ShouldBe(1);
        tenants[0].TenantName.ShouldBe(Constants.DefaultTenantName);
        tenants[0].ConnectionStrings.EdFiAdminConnectionString.ShouldBe("admin-conn-default");
        tenants[0].ConnectionStrings.EdFiSecurityConnectionString.ShouldBe("security-conn-default");
    }

    [Test]
    public async Task GetTenantByTenantIdAsync_Should_Return_Correct_Tenant()
    {
        var service = new TenantService(_options, _memoryCache);

        var tenant = await service.GetTenantByTenantIdAsync("tenantA");

        tenant.ShouldNotBeNull();
        tenant!.TenantName.ShouldBe("tenantA");
        tenant.ConnectionStrings.EdFiAdminConnectionString.ShouldBe("admin-conn-A");
        tenant.ConnectionStrings.EdFiSecurityConnectionString.ShouldBe("security-conn-A");
    }

    [Test]
    public async Task GetTenantByTenantIdAsync_Should_Return_Null_If_Not_Found()
    {
        var service = new TenantService(_options, _memoryCache);

        var tenant = await service.GetTenantByTenantIdAsync("notfound");

        tenant.ShouldBeNull();
    }

    [Test]
    public async Task InitializeTenantsAsync_Should_Store_Tenants_In_Cache()
    {
        var service = new TenantService(_options, _memoryCache);

        await service.InitializeTenantsAsync();

        var cached = _memoryCache.Get<List<TenantModel>>(Constants.TenantsCacheKey);
        cached.ShouldNotBeNull();
        cached!.Count.ShouldBe(2);
    }

    [Test]
    public async Task GetTenantsAsync_Should_Return_From_Cache_If_Requested()
    {
        var service = new TenantService(_options, _memoryCache);

        // Prime the cache
        await service.InitializeTenantsAsync();

        // Remove a tenant from the underlying settings to prove cache is used
        _appSettings.Tenants.Remove("tenantA");

        var tenants = await service.GetTenantsAsync(fromCache: true);

        tenants.Count.ShouldBe(2);
        tenants.Any(t => t.TenantName == "tenantA").ShouldBeTrue();
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_Should_Return_Correct_TenantDetails()
    {
        var service = new TenantService(_options, _memoryCache);
        var tenantName = "tenantA";

        var odsInstance = new OdsInstance
        {
            OdsInstanceId = 101,
            Name = "Test Instance"
        };

        var educationOrganization = new EducationOrganization
        {
            InstanceId = 101,
            InstanceName = "Test Instance",
            EducationOrganizationId = 100,
            NameOfInstitution = "Test School",
            ShortNameOfInstitution = "Test",
            Discriminator = "School"
        };

        var tenantOdsInstanceModels = new List<TenantOdsInstanceModel>
        {
            new()
            {
                OdsInstanceId = 101,
                Name = "Test Instance"
            }
        };

        var tenantEducationOrganizationModels = new List<TenantEducationOrganizationModel>
        {
            new()
            {
                InstanceId = 101,
                InstanceName = "Test Instance",
                EducationOrganizationId = 100,
                NameOfInstitution = "Test School",
                ShortNameOfInstitution = "Test",
                Discriminator = "School"
            }
        };

        A.CallTo(() => _getOdsInstancesQuery.Execute()).Returns([odsInstance]);
        A.CallTo(() => _getEducationOrganizationQuery.Execute(101)).Returns([educationOrganization]);
        A.CallTo(() => _mapper.Map<List<TenantOdsInstanceModel>>(A<List<OdsInstance>>._)).Returns(tenantOdsInstanceModels);
        A.CallTo(() => _mapper.Map<List<TenantEducationOrganizationModel>>(A<List<EducationOrganization>>._)).Returns(tenantEducationOrganizationModels);

        var tenant = await service.GetTenantEdOrgsByInstancesAsync(_getOdsInstancesQuery, _getEducationOrganizationQuery, _mapper, tenantName);

        tenant.ShouldNotBeNull();
        tenant!.TenantName.ShouldBe(tenantName);
        tenant.OdsInstances.ShouldNotBeNull();
        tenant.OdsInstances!.Count.ShouldBe(1);
        tenant.OdsInstances[0].OdsInstanceId.ShouldBe(odsInstance.OdsInstanceId);
        tenant.OdsInstances[0].Name.ShouldBe(odsInstance.Name);
        tenant.OdsInstances[0].EducationOrganizations.ShouldNotBeNull();
        tenant.OdsInstances[0].EducationOrganizations!.Count.ShouldBe(1);
        tenant.OdsInstances[0].EducationOrganizations[0].InstanceId.ShouldBe(educationOrganization.InstanceId);
        tenant.OdsInstances[0].EducationOrganizations[0].InstanceName.ShouldBe(educationOrganization.InstanceName);
        tenant.OdsInstances[0].EducationOrganizations[0].EducationOrganizationId.ShouldBe(educationOrganization.EducationOrganizationId);
        tenant.OdsInstances[0].EducationOrganizations[0].NameOfInstitution.ShouldBe(educationOrganization.NameOfInstitution);
        tenant.OdsInstances[0].EducationOrganizations[0].ShortNameOfInstitution.ShouldBe(educationOrganization.ShortNameOfInstitution);
        tenant.OdsInstances[0].EducationOrganizations[0].Discriminator.ShouldBe(educationOrganization.Discriminator);
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_Should_Return_Null_If_Not_Found()
    {
        var service = new TenantService(_options, _memoryCache);

        var tenant = await service.GetTenantEdOrgsByInstancesAsync(_getOdsInstancesQuery, _getEducationOrganizationQuery, _mapper, "notfound");

        tenant.ShouldBeNull();
    }
}
