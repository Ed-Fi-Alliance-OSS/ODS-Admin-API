// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using EdFi.Common.Configuration;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Tenants;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using FakeItEasy;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Tenants;

[TestFixture]
public class ReadTenantsTest
{
    private IGetOdsInstancesQuery _getOdsInstancesQuery = null!;
    private IGetEducationOrganizationQuery _getEducationOrganizationQuery = null!;
    private IMapper _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        _mapper = A.Fake<IMapper>();
        _getOdsInstancesQuery = A.Fake<IGetOdsInstancesQuery>();
        _getEducationOrganizationQuery = A.Fake<IGetEducationOrganizationQuery>();
    }

    [Test]
    public async Task GetTenantsByTenantIdAsync_ReturnsOk_WhenTenantExists()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        var tenantName = "tenant1";

        var tenant = new TenantModel
        {
            TenantName = tenantName,
            ConnectionStrings = new TenantModelConnectionStrings
            {
                EdFiAdminConnectionString = "Host=adminhost;Database=admindb;",
                EdFiSecurityConnectionString = "Host=sechost;Database=secdb;"
            }
        };

        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres" });
        A.CallTo(() => tenantsService.GetTenantByTenantIdAsync(tenantName)).Returns(tenant);

        var result = await ReadTenants.GetTenantsByTenantIdAsync(tenantsService, memoryCache, tenantName, options);

        result.ShouldNotBeNull();
        var okResult = result as IResult;
        okResult.ShouldNotBeNull();
    }

    [Test]
    public async Task GetTenantsByTenantIdAsync_ReturnsNotFound_WhenTenantDoesNotExist()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        var tenantName = "missingTenant";

        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres" });
        A.CallTo(() => tenantsService.GetTenantByTenantIdAsync(tenantName)).Returns((TenantModel)null);

        var result = await ReadTenants.GetTenantsByTenantIdAsync(tenantsService, memoryCache, tenantName, options);

        result.ShouldNotBeNull();
        var notFoundResult = result as IResult;
        notFoundResult.ShouldNotBeNull();
    }

    [Test]
    public async Task GetTenantsAsync_ReturnsOk_WithTenantList()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        var databaseEngine = DatabaseEngine.Postgres;

        var tenants = new List<TenantModel>
        {
                new() {
                    TenantName = "tenant1",
                    ConnectionStrings = new TenantModelConnectionStrings
                    {
                        EdFiAdminConnectionString = "Host=adminhost;Database=admindb;",
                        EdFiSecurityConnectionString = "Host=sechost;Database=secdb;"
                    }
                }
            };

        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres" });
        A.CallTo(() => tenantsService.GetTenantsAsync(true)).Returns(Task.FromResult(tenants));

        var result = await ReadTenants.GetTenantsAsync(tenantsService, memoryCache, options);

        result.ShouldNotBeNull();
        var okResult = result as IResult;
        okResult.ShouldNotBeNull();
    }

    [Test]
    public void GetTenantsByTenantIdAsync_ThrowsNotFoundException_WhenDatabaseEngineIsNull()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        var tenantName = "tenant1";

        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = null });

        Should.ThrowAsync<NotFoundException<string>>(async () =>
        {
            await ReadTenants.GetTenantsByTenantIdAsync(tenantsService, memoryCache, tenantName, options);
        });
    }

    [Test]
    public void GetTenantsAsync_ThrowsNotFoundException_WhenDatabaseEngineIsNull()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();

        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = null });

        Should.ThrowAsync<NotFoundException<string>>(async () =>
        {
            await ReadTenants.GetTenantsAsync(tenantsService, memoryCache, options);
        });
    }

    [Test]
    public async Task GetTenantDetailsAsync_ReturnsOk_WhenTenantExists()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        var tenantHeader = "tenant1";

        var educationOrganization = new TenantEducationOrganizationModel()
        {
            InstanceId = 1,
            InstanceName = "instance name 1",
            NameOfInstitution = "name of institution 1",
            ShortNameOfInstitution = "short name of institution 1",
            Discriminator = "discriminator 1"
        };

        var odsInstance = new TenantOdsInstanceModel()
        {
            OdsInstanceId = 1,
            EducationOrganizations = [educationOrganization]
        };

        var tenantDetailModel = new TenantDetailModel()
        {
            TenantName = tenantHeader,
            OdsInstances = [odsInstance]
        };

        var request = A.Fake<HttpRequest>();
        A.CallTo(() => request.Headers["tenant"]).Returns(new StringValues(tenantHeader));
        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres", MultiTenancy = true });
        A.CallTo(() => tenantsService.GetTenantDetailsAsync(_getOdsInstancesQuery, _getEducationOrganizationQuery, _mapper, tenantHeader)).Returns(tenantDetailModel);

        var result = await ReadTenants.GetTenantDetailsAsync(request, tenantsService, _getOdsInstancesQuery, _getEducationOrganizationQuery, _mapper, memoryCache, options);

        result.ShouldNotBeNull();
    }

    [Test]
    public void GetTenantDetailsAsync_ThrowsValidationException_WhenTenantHeaderIsNull()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();

        var request = A.Fake<HttpRequest>();
        A.CallTo(() => request.Headers["tenant"]).Returns(StringValues.Empty);
        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres", MultiTenancy = true });

        Should.ThrowAsync<ValidationException>(async () =>
        {
            await ReadTenants.GetTenantDetailsAsync(request, tenantsService, _getOdsInstancesQuery, _getEducationOrganizationQuery, _mapper, memoryCache, options);
        });
    }
}
