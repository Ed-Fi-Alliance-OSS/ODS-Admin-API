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
using EdFi.Ods.AdminApi.V3.Features.DataStores;
using EdFi.Ods.AdminApi.V3.Features.Tenants;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Tenants;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using FakeItEasy;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using OdsInstance = EdFi.Admin.DataAccess.Models.OdsInstance;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Services.Tenants;

[TestFixture]
internal class TenantServiceTests
{
    private IOptionsSnapshot<AppSettingsFile> _options = null!;
    private IMemoryCache _memoryCache = null!;
    private AppSettingsFile _appSettings = null!;
    private IGetDataStoresQuery _getDataStoresQuery = null!;
    private IGetEducationOrganizationQuery _getEducationOrganizationQuery = null!;
    private IGetDbDataStoresQuery _getDbDataStoresQuery = null!;

    [SetUp]
    public void SetUp()
    {
        _options = A.Fake<IOptionsSnapshot<AppSettingsFile>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _getDataStoresQuery = A.Fake<IGetDataStoresQuery>();
        _getEducationOrganizationQuery = A.Fake<IGetEducationOrganizationQuery>();
        _getDbDataStoresQuery = A.Fake<IGetDbDataStoresQuery>();
        A.CallTo(() => _getDbDataStoresQuery.Execute(A<CommonQueryParams>._, A<int?>._, A<string>.Ignored)).Returns([]);
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

        var tenantOdsInstanceModels = new List<TenantDataStoreModel>
        {
            new()
            {
                DataStoreId = 101,
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

        A.CallTo(() => _getDataStoresQuery.Execute()).Returns([odsInstance]);
        A.CallTo(() => _getEducationOrganizationQuery.Execute(A<int[]>.That.Matches(ids => ids.Length == 1 && ids[0] == 101)))
            .Returns([educationOrganization]);

        var tenant = await service.GetTenantEdOrgsByInstancesAsync(_getDataStoresQuery, _getEducationOrganizationQuery, _getDbDataStoresQuery, tenantName);

        tenant.ShouldNotBeNull();
        tenant!.TenantName.ShouldBe(tenantName);
        tenant.DataStores.ShouldNotBeNull();
        tenant.DataStores!.Count.ShouldBe(1);
        tenant.DataStores[0].DataStoreId.ShouldBe(odsInstance.OdsInstanceId);
        tenant.DataStores[0].Name.ShouldBe(odsInstance.Name);
        tenant.DataStores[0].EducationOrganizations.ShouldNotBeNull();
        tenant.DataStores[0].EducationOrganizations!.Count.ShouldBe(1);
        tenant.DataStores[0].EducationOrganizations[0].EducationOrganizationId.ShouldBe(educationOrganization.EducationOrganizationId);
        tenant.DataStores[0].EducationOrganizations[0].NameOfInstitution.ShouldBe(educationOrganization.NameOfInstitution);
        tenant.DataStores[0].EducationOrganizations[0].ShortNameOfInstitution.ShouldBe(educationOrganization.ShortNameOfInstitution);
        tenant.DataStores[0].EducationOrganizations[0].Discriminator.ShouldBe(educationOrganization.Discriminator);
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_Should_Return_Null_If_Not_Found()
    {
        var service = new TenantService(_options, _memoryCache);

        var tenant = await service.GetTenantEdOrgsByInstancesAsync(_getDataStoresQuery, _getEducationOrganizationQuery, _getDbDataStoresQuery, "notfound");

        tenant.ShouldBeNull();
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_SetsStatusCreated_WhenDataStoreHasNoLinkedDbDataStore()
    {
        _appSettings.AppSettings.MultiTenancy = false;
        var service = new TenantService(_options, _memoryCache);

        var odsInstance = new OdsInstance { OdsInstanceId = 1, Name = "DataStore1" };
        A.CallTo(() => _getDataStoresQuery.Execute()).Returns([odsInstance]);

        var result = await service.GetTenantEdOrgsByInstancesAsync(
            _getDataStoresQuery, _getEducationOrganizationQuery, _getDbDataStoresQuery, Constants.DefaultTenantName);

        result.ShouldNotBeNull();
        result!.DataStores.Count.ShouldBe(1);
        result.DataStores[0].Status.ShouldBe(DbInstanceStatus.Created.ToString());
        result.DataStores[0].DatabaseTemplate.ShouldBeNull();
        result.DataStores[0].DatabaseName.ShouldBeNull();
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_EnrichesDataStore_WithLinkedDbDataStoreFields()
    {
        _appSettings.AppSettings.MultiTenancy = false;
        var service = new TenantService(_options, _memoryCache);

        var odsInstance = new OdsInstance { OdsInstanceId = 2, Name = "DataStore2" };
        A.CallTo(() => _getDataStoresQuery.Execute()).Returns([odsInstance]);

        var dbDataStore = new DbInstance
        {
            Id = 10,
            Name = "DbDataStore2",
            OdsInstanceId = 2,
            Status = DbInstanceStatus.CreateInProgress.ToString(),
            DatabaseTemplate = "Minimal",
            DatabaseName = "EdFi_ODS_2",
            LastRefreshed = System.DateTime.UtcNow
        };
        A.CallTo(() => _getDbDataStoresQuery.Execute(A<CommonQueryParams>._, A<int?>._, A<string>.Ignored))
            .Returns([dbDataStore]);

        var result = await service.GetTenantEdOrgsByInstancesAsync(
            _getDataStoresQuery, _getEducationOrganizationQuery, _getDbDataStoresQuery, Constants.DefaultTenantName);

        result.ShouldNotBeNull();
        result!.DataStores.Count.ShouldBe(1);
        var dataStore = result.DataStores[0];
        dataStore.Status.ShouldBe(DbInstanceStatus.CreateInProgress.ToString());
        dataStore.DatabaseTemplate.ShouldBe("Minimal");
        dataStore.DatabaseName.ShouldBe("EdFi_ODS_2");
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_AddsUnlinkedDbDataStores_WithSuccessiveNegativeIds()
    {
        _appSettings.AppSettings.MultiTenancy = false;
        var service = new TenantService(_options, _memoryCache);

        A.CallTo(() => _getDataStoresQuery.Execute()).Returns([]);

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
        A.CallTo(() => _getDbDataStoresQuery.Execute(A<CommonQueryParams>._, A<int?>._, A<string>.Ignored))
            .Returns([unlinked1, unlinked2]);

        var result = await service.GetTenantEdOrgsByInstancesAsync(
            _getDataStoresQuery, _getEducationOrganizationQuery, _getDbDataStoresQuery, Constants.DefaultTenantName);

        result.ShouldNotBeNull();
        result!.DataStores.Count.ShouldBe(2);
        result.DataStores.ShouldContain(d => d.DataStoreId == -1 && d.Name == "Unlinked-A");
        result.DataStores.ShouldContain(d => d.DataStoreId == -2 && d.Name == "Unlinked-B");
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_MixedScenario_LinkedAndUnlinked()
    {
        _appSettings.AppSettings.MultiTenancy = false;
        var service = new TenantService(_options, _memoryCache);

        var odsInstance = new OdsInstance { OdsInstanceId = 5, Name = "DataStore5" };
        A.CallTo(() => _getDataStoresQuery.Execute()).Returns([odsInstance]);

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
        A.CallTo(() => _getDbDataStoresQuery.Execute(A<CommonQueryParams>._, A<int?>._, A<string>.Ignored))
            .Returns([linked, unlinked]);

        var result = await service.GetTenantEdOrgsByInstancesAsync(
            _getDataStoresQuery, _getEducationOrganizationQuery, _getDbDataStoresQuery, Constants.DefaultTenantName);

        result.ShouldNotBeNull();
        result!.DataStores.Count.ShouldBe(2);

        var linkedDataStore = result.DataStores.Single(d => d.DataStoreId == 5);
        linkedDataStore.Status.ShouldBe(DbInstanceStatus.Created.ToString());
        linkedDataStore.DatabaseTemplate.ShouldBe("Minimal");
        linkedDataStore.DatabaseName.ShouldBe("EdFi_ODS_5");

        var unlinkedDataStore = result.DataStores.Single(d => d.DataStoreId == -1);
        unlinkedDataStore.Name.ShouldBe("Unlinked-C");
        unlinkedDataStore.Status.ShouldBe(DbInstanceStatus.PendingCreate.ToString());
    }
}





