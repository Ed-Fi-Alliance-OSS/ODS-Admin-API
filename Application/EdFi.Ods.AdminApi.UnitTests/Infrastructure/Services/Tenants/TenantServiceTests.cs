// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using Constants = EdFi.Ods.AdminApi.Common.Constants.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.OdsInstances;
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
    private IGetDbInstancesQuery _getDbInstancesQuery = null!;

    [SetUp]
    public void SetUp()
    {
        _options = A.Fake<IOptionsSnapshot<AppSettingsFile>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _getOdsInstancesQuery = A.Fake<IGetOdsInstancesQuery>();
        _getEducationOrganizationQuery = A.Fake<IGetEducationOrganizationQuery>();
        _getDbInstancesQuery = A.Fake<IGetDbInstancesQuery>();
        A.CallTo(() => _getDbInstancesQuery.Execute(A<CommonQueryParams>._, A<int?>._, A<string>.Ignored))
            .Returns([]);
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

        var tenantEducationOrganizationModels = new List<EducationOrganizationModel>
        {
            new()
            {
                EducationOrganizationId = 100,
                NameOfInstitution = "Test School",
                ShortNameOfInstitution = "Test",
                Discriminator = "School"
            }
        };

        A.CallTo(() => _getOdsInstancesQuery.Execute()).Returns([odsInstance]);
        A.CallTo(() => _getEducationOrganizationQuery.Execute(A<int[]>.That.Matches(ids => ids.Length == 1 && ids[0] == 101)))
            .Returns([educationOrganization]);

        var tenant = await service.GetTenantEdOrgsByInstancesAsync(_getOdsInstancesQuery, _getEducationOrganizationQuery, _getDbInstancesQuery, tenantName);

        tenant.ShouldNotBeNull();
        tenant!.TenantName.ShouldBe(tenantName);
        tenant.OdsInstances.ShouldNotBeNull();
        tenant.OdsInstances!.Count.ShouldBe(1);
        tenant.OdsInstances[0].OdsInstanceId.ShouldBe(odsInstance.OdsInstanceId);
        tenant.OdsInstances[0].Name.ShouldBe(odsInstance.Name);
        tenant.OdsInstances[0].EducationOrganizations.ShouldNotBeNull();
        tenant.OdsInstances[0].EducationOrganizations!.Count.ShouldBe(1);
        tenant.OdsInstances[0].EducationOrganizations[0].EducationOrganizationId.ShouldBe(educationOrganization.EducationOrganizationId);
        tenant.OdsInstances[0].EducationOrganizations[0].NameOfInstitution.ShouldBe(educationOrganization.NameOfInstitution);
        tenant.OdsInstances[0].EducationOrganizations[0].ShortNameOfInstitution.ShouldBe(educationOrganization.ShortNameOfInstitution);
        tenant.OdsInstances[0].EducationOrganizations[0].Discriminator.ShouldBe(educationOrganization.Discriminator);
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_Should_Return_Null_If_Not_Found()
    {
        var service = new TenantService(_options, _memoryCache);

        var tenant = await service.GetTenantEdOrgsByInstancesAsync(_getOdsInstancesQuery, _getEducationOrganizationQuery, _getDbInstancesQuery, "notfound");

        tenant.ShouldBeNull();
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_SetsStatusCreated_WhenOdsInstanceHasNoLinkedDbInstance()
    {
        _appSettings.AppSettings.MultiTenancy = false;
        var service = new TenantService(_options, _memoryCache);

        var odsInstance = new OdsInstance { OdsInstanceId = 1, Name = "Instance1" };
        A.CallTo(() => _getOdsInstancesQuery.Execute()).Returns([odsInstance]);

        var result = await service.GetTenantEdOrgsByInstancesAsync(
            _getOdsInstancesQuery, _getEducationOrganizationQuery, _getDbInstancesQuery, Constants.DefaultTenantName);

        result.ShouldNotBeNull();
        result!.OdsInstances.Count.ShouldBe(1);
        result.OdsInstances[0].Status.ShouldBe(DbInstanceStatus.Created.ToString());
        result.OdsInstances[0].DbInstanceId.ShouldBeNull();
        result.OdsInstances[0].DatabaseTemplate.ShouldBeNull();
        result.OdsInstances[0].DatabaseName.ShouldBeNull();
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_EnrichesOdsInstance_WithLinkedDbInstanceFields()
    {
        _appSettings.AppSettings.MultiTenancy = false;
        var service = new TenantService(_options, _memoryCache);

        var odsInstance = new OdsInstance { OdsInstanceId = 2, Name = "Instance2" };
        A.CallTo(() => _getOdsInstancesQuery.Execute()).Returns([odsInstance]);

        var dbInstance = new DbInstance
        {
            Id = 10,
            Name = "DbInstance2",
            OdsInstanceId = 2,
            Status = DbInstanceStatus.CreateInProgress.ToString(),
            DatabaseTemplate = "Minimal",
            DatabaseName = "EdFi_ODS_2",
            LastRefreshed = System.DateTime.UtcNow
        };
        A.CallTo(() => _getDbInstancesQuery.Execute(A<CommonQueryParams>._, A<int?>._, A<string>.Ignored))
            .Returns([dbInstance]);

        var result = await service.GetTenantEdOrgsByInstancesAsync(
            _getOdsInstancesQuery, _getEducationOrganizationQuery, _getDbInstancesQuery, Constants.DefaultTenantName);

        result.ShouldNotBeNull();
        result!.OdsInstances.Count.ShouldBe(1);
        var instance = result.OdsInstances[0];
        instance.DbInstanceId.ShouldBe(10);
        instance.Status.ShouldBe(DbInstanceStatus.CreateInProgress.ToString());
        instance.DatabaseTemplate.ShouldBe("Minimal");
        instance.DatabaseName.ShouldBe("EdFi_ODS_2");
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_AddsUnlinkedDbInstances_WithSuccessiveNegativeIds()
    {
        _appSettings.AppSettings.MultiTenancy = false;
        var service = new TenantService(_options, _memoryCache);

        A.CallTo(() => _getOdsInstancesQuery.Execute()).Returns([]);

        var unlinked1 = new DbInstance
        {
            Id = 20, Name = "Unlinked-A", OdsInstanceId = null,
            Status = DbInstanceStatus.PendingCreate.ToString(),
            DatabaseTemplate = "Sample", LastRefreshed = System.DateTime.UtcNow
        };
        var unlinked2 = new DbInstance
        {
            Id = 21, Name = "Unlinked-B", OdsInstanceId = null,
            Status = DbInstanceStatus.PendingCreate.ToString(),
            DatabaseTemplate = "Minimal", LastRefreshed = System.DateTime.UtcNow
        };
        A.CallTo(() => _getDbInstancesQuery.Execute(A<CommonQueryParams>._, A<int?>._, A<string>.Ignored))
            .Returns([unlinked1, unlinked2]);

        var result = await service.GetTenantEdOrgsByInstancesAsync(
            _getOdsInstancesQuery, _getEducationOrganizationQuery, _getDbInstancesQuery, Constants.DefaultTenantName);

        result.ShouldNotBeNull();
        result!.OdsInstances.Count.ShouldBe(2);
        result.OdsInstances.ShouldContain(i => i.OdsInstanceId == -1 && i.Name == "Unlinked-A" && i.DbInstanceId == 20);
        result.OdsInstances.ShouldContain(i => i.OdsInstanceId == -2 && i.Name == "Unlinked-B" && i.DbInstanceId == 21);
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_MixedScenario_LinkedAndUnlinked()
    {
        _appSettings.AppSettings.MultiTenancy = false;
        var service = new TenantService(_options, _memoryCache);

        var odsInstance = new OdsInstance { OdsInstanceId = 5, Name = "Instance5" };
        A.CallTo(() => _getOdsInstancesQuery.Execute()).Returns([odsInstance]);

        var linked = new DbInstance
        {
            Id = 30, Name = "Linked-5", OdsInstanceId = 5,
            Status = DbInstanceStatus.Created.ToString(),
            DatabaseTemplate = "Minimal", DatabaseName = "EdFi_ODS_5",
            LastRefreshed = System.DateTime.UtcNow
        };
        var unlinked = new DbInstance
        {
            Id = 31, Name = "Unlinked-C", OdsInstanceId = null,
            Status = DbInstanceStatus.PendingCreate.ToString(),
            DatabaseTemplate = "Sample", LastRefreshed = System.DateTime.UtcNow
        };
        A.CallTo(() => _getDbInstancesQuery.Execute(A<CommonQueryParams>._, A<int?>._, A<string>.Ignored))
            .Returns([linked, unlinked]);

        var result = await service.GetTenantEdOrgsByInstancesAsync(
            _getOdsInstancesQuery, _getEducationOrganizationQuery, _getDbInstancesQuery, Constants.DefaultTenantName);

        result.ShouldNotBeNull();
        result!.OdsInstances.Count.ShouldBe(2);

        var linkedInstance = result.OdsInstances.Single(i => i.OdsInstanceId == 5);
        linkedInstance.DbInstanceId.ShouldBe(30);
        linkedInstance.Status.ShouldBe(DbInstanceStatus.Created.ToString());
        linkedInstance.DatabaseTemplate.ShouldBe("Minimal");
        linkedInstance.DatabaseName.ShouldBe("EdFi_ODS_5");

        var unlinkedInstance = result.OdsInstances.Single(i => i.OdsInstanceId == -1);
        unlinkedInstance.DbInstanceId.ShouldBe(31);
        unlinkedInstance.Name.ShouldBe("Unlinked-C");
        unlinkedInstance.Status.ShouldBe(DbInstanceStatus.PendingCreate.ToString());
    }
}
