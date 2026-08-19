// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Features.Tenants;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.OdsInstances;
using EdFi.Ods.AdminApi.Features.Tenants;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using FakeItEasy;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
    private IGetOdsInstanceManagesQuery _getOdsInstanceManagesQuery = null!;

    [SetUp]
    public void SetUp()
    {
        _getOdsInstancesQuery = A.Fake<IGetOdsInstancesQuery>();
        _getEducationOrganizationQuery = A.Fake<IGetEducationOrganizationQuery>();
        _getOdsInstanceManagesQuery = A.Fake<IGetOdsInstanceManagesQuery>();
        A.CallTo(() => _getOdsInstanceManagesQuery.Execute(A<CommonQueryParams>._, A<int?>._, A<string>.Ignored))
            .Returns([]);
    }

    [Test]
    public async Task GetTenantsAsync_ReturnsOk_WithMappedConnectionStrings()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "PostgreSql", MultiTenancy = true });

        var tenants = new List<TenantModel>
        {
            new()
            {
                TenantName = "tenant1",
                ConnectionStrings = new TenantModelConnectionStrings(
                    "Host=admin-host;Database=admin-db;",
                    "Host=security-host;Database=security-db;")
            }
        };
        A.CallTo(() => tenantsService.GetTenantsAsync(true)).Returns(tenants);

        var result = await ReadTenants.GetTenantsAsync(tenantsService, memoryCache, options);

        var ok = result as Ok<List<TenantsResponse>>;
        ok.ShouldNotBeNull();
        ok.Value.ShouldNotBeNull();
        ok.Value.Count.ShouldBe(1);
        ok.Value[0].TenantName.ShouldBe("tenant1");
        ok.Value[0].AdminConnectionString!.host.ShouldBe("admin-host");
        ok.Value[0].AdminConnectionString!.database.ShouldBe("admin-db");
        ok.Value[0].SecurityConnectionString!.host.ShouldBe("security-host");
        ok.Value[0].SecurityConnectionString!.database.ShouldBe("security-db");
    }

    [Test]
    public async Task GetTenantsByTenantIdAsync_ReturnsOk_WhenTenantExists()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "PostgreSql", MultiTenancy = true });

        var tenant = new TenantModel
        {
            TenantName = "tenant1",
            ConnectionStrings = new TenantModelConnectionStrings(
                "Host=admin-host;Database=admin-db;",
                "Host=security-host;Database=security-db;")
        };
        A.CallTo(() => tenantsService.GetTenantByTenantIdAsync("tenant1")).Returns(tenant);

        var result = await ReadTenants.GetTenantsByTenantIdAsync(tenantsService, memoryCache, "tenant1", options);

        var ok = result as Ok<TenantsResponse>;
        ok.ShouldNotBeNull();
        ok.Value.ShouldNotBeNull();
        ok.Value!.TenantName.ShouldBe("tenant1");
        ok.Value.AdminConnectionString!.host.ShouldBe("admin-host");
        ok.Value.AdminConnectionString!.database.ShouldBe("admin-db");
        ok.Value.SecurityConnectionString!.host.ShouldBe("security-host");
        ok.Value.SecurityConnectionString!.database.ShouldBe("security-db");
    }

    [Test]
    public async Task GetTenantsByTenantIdAsync_ReturnsNotFound_WhenTenantDoesNotExist()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres", MultiTenancy = true });
        A.CallTo(() => tenantsService.GetTenantByTenantIdAsync("notfound")).Returns((TenantModel)null!);

        var result = await ReadTenants.GetTenantsByTenantIdAsync(tenantsService, memoryCache, "notfound", options);

        result.ShouldBeOfType<NotFound>();
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_ReturnsOk_WhenTenantExists()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        var swaggerOptions = A.Fake<IOptions<SwaggerSettings>>();
        string tenantName = "tenant1", tenantHeader = "tenant1";

        var educationOrganization = new EducationOrganizationModel()
        {
            EducationOrganizationId = 1001,
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
            TenantName = tenantName,
            OdsInstances = [odsInstance]
        };

        var request = A.Fake<HttpRequest>();
        var headers = A.Fake<IHeaderDictionary>();
        A.CallTo(() => request.Headers).Returns(headers);
        A.CallTo(() => headers["tenant"]).Returns(new StringValues(tenantHeader));
        A.CallTo(() => headers.Referer).Returns(StringValues.Empty);
        A.CallTo(() => request.Path).Returns(new PathString("/tenants/tenant1/OdsInstances/edOrgs"));
        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres", MultiTenancy = true });
        A.CallTo(() => swaggerOptions.Value).Returns(new SwaggerSettings { EnableSwagger = true });
        A.CallTo(() => tenantsService.GetTenantEdOrgsByInstancesAsync(_getOdsInstancesQuery, _getEducationOrganizationQuery, _getOdsInstanceManagesQuery, tenantName)).Returns(tenantDetailModel);

        var result = await ReadTenants.GetTenantEdOrgsByInstancesAsync(request, tenantsService, _getOdsInstancesQuery, _getEducationOrganizationQuery, _getOdsInstanceManagesQuery, memoryCache, options, swaggerOptions, tenantName);

        result.ShouldNotBeNull();
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_ReturnsNullId_WhenOdsInstanceIdIsNull()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        var swaggerOptions = A.Fake<IOptions<SwaggerSettings>>();
        string tenantName = "tenant1", tenantHeader = "tenant1";

        var tenantDetailModel = new TenantDetailModel
        {
            TenantName = tenantName,
            OdsInstances =
            [
                new TenantOdsInstanceModel
                {
                    OdsInstanceId = null,
                    Name = "Unlinked",
                    EducationOrganizations = []
                }
            ]
        };

        var request = A.Fake<HttpRequest>();
        var headers = A.Fake<IHeaderDictionary>();
        A.CallTo(() => request.Headers).Returns(headers);
        A.CallTo(() => headers["tenant"]).Returns(new StringValues(tenantHeader));
        A.CallTo(() => headers.Referer).Returns(StringValues.Empty);
        A.CallTo(() => request.Path).Returns(new PathString("/tenants/tenant1/OdsInstances/edOrgs"));
        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres", MultiTenancy = true });
        A.CallTo(() => swaggerOptions.Value).Returns(new SwaggerSettings { EnableSwagger = true });
        A.CallTo(() => tenantsService.GetTenantEdOrgsByInstancesAsync(_getOdsInstancesQuery, _getEducationOrganizationQuery, _getOdsInstanceManagesQuery, tenantName)).Returns(tenantDetailModel);

        var result = await ReadTenants.GetTenantEdOrgsByInstancesAsync(
            request,
            tenantsService,
            _getOdsInstancesQuery,
            _getEducationOrganizationQuery,
            _getOdsInstanceManagesQuery,
            memoryCache,
            options,
            swaggerOptions,
            tenantName);

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<TenantDetailsResponse>;
        ok.ShouldNotBeNull();
        ok.Value.ShouldNotBeNull();
        ok.Value.OdsInstances!.Count.ShouldBe(1);
        ok.Value.OdsInstances[0].OdsInstanceId.ShouldBeNull();
    }

    [Test]
    public void GetTenantEdOrgsByInstancesAsync_ThrowsValidationException_WhenTenantHeaderAndTenantNameAreDifferent()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        var swaggerOptions = A.Fake<IOptions<SwaggerSettings>>();
        string tenantName = "tenant1", tenantHeader = "tenant2";

        var request = A.Fake<HttpRequest>();
        var headers = A.Fake<IHeaderDictionary>();
        A.CallTo(() => request.Headers).Returns(headers);
        A.CallTo(() => headers["tenant"]).Returns(tenantHeader);
        A.CallTo(() => headers.Referer).Returns(StringValues.Empty);
        A.CallTo(() => request.Path).Returns(new PathString("/tenants/tenant1/OdsInstances/edOrgs"));
        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres", MultiTenancy = true });
        A.CallTo(() => swaggerOptions.Value).Returns(new SwaggerSettings { EnableSwagger = true });

        Should.ThrowAsync<ValidationException>(async () =>
        {
            await ReadTenants.GetTenantEdOrgsByInstancesAsync(request, tenantsService, _getOdsInstancesQuery, _getEducationOrganizationQuery, _getOdsInstanceManagesQuery, memoryCache, options, swaggerOptions, tenantName);
        });
    }

    [Test]
    public void GetTenantEdOrgsByInstancesAsync_ThrowsValidationException_WhenTenantHeaderIsEmpty()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        var swaggerOptions = A.Fake<IOptions<SwaggerSettings>>();
        string tenantName = "tenant1";

        var request = A.Fake<HttpRequest>();
        var headers = A.Fake<IHeaderDictionary>();
        A.CallTo(() => request.Headers).Returns(headers);
        A.CallTo(() => headers["tenant"]).Returns(StringValues.Empty);
        A.CallTo(() => headers.Referer).Returns(StringValues.Empty);
        A.CallTo(() => request.Path).Returns(new PathString("/tenants/tenant1/OdsInstances/edOrgs"));
        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres", MultiTenancy = true });
        A.CallTo(() => swaggerOptions.Value).Returns(new SwaggerSettings { EnableSwagger = true });

        Should.ThrowAsync<ValidationException>(async () =>
        {
            await ReadTenants.GetTenantEdOrgsByInstancesAsync(request, tenantsService, _getOdsInstancesQuery, _getEducationOrganizationQuery, _getOdsInstanceManagesQuery, memoryCache, options, swaggerOptions, tenantName);
        });
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_SkipsTenantHeaderValidation_WhenRequestPathContainsSwagger()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        var swaggerOptions = A.Fake<IOptions<SwaggerSettings>>();
        string tenantName = "tenant1";

        var tenantDetailModel = new TenantDetailModel()
        {
            TenantName = tenantName,
            OdsInstances = []
        };

        var request = A.Fake<HttpRequest>();
        var headers = A.Fake<IHeaderDictionary>();
        A.CallTo(() => request.Headers).Returns(headers);
        A.CallTo(() => headers["tenant"]).Returns(StringValues.Empty);
        A.CallTo(() => headers.Referer).Returns(StringValues.Empty);
        A.CallTo(() => request.Path).Returns(new PathString("/swagger/index.html"));
        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres", MultiTenancy = true });
        A.CallTo(() => swaggerOptions.Value).Returns(new SwaggerSettings { EnableSwagger = true });
        A.CallTo(() => tenantsService.GetTenantEdOrgsByInstancesAsync(_getOdsInstancesQuery, _getEducationOrganizationQuery, _getOdsInstanceManagesQuery, tenantName)).Returns(tenantDetailModel);

        var result = await ReadTenants.GetTenantEdOrgsByInstancesAsync(request, tenantsService, _getOdsInstancesQuery, _getEducationOrganizationQuery, _getOdsInstanceManagesQuery, memoryCache, options, swaggerOptions, tenantName);

        result.ShouldNotBeNull();
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_SkipsTenantHeaderValidation_WhenRefererContainsSwagger()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        var swaggerOptions = A.Fake<IOptions<SwaggerSettings>>();
        string tenantName = "tenant1";

        var tenantDetailModel = new TenantDetailModel()
        {
            TenantName = tenantName,
            OdsInstances = []
        };

        var request = A.Fake<HttpRequest>();
        var headers = A.Fake<IHeaderDictionary>();
        A.CallTo(() => request.Headers).Returns(headers);
        A.CallTo(() => headers["tenant"]).Returns(StringValues.Empty);
        A.CallTo(() => headers.Referer).Returns(new StringValues("https://localhost/swagger/index.html"));
        A.CallTo(() => request.Path).Returns(new PathString("/tenants/tenant1/OdsInstances/edOrgs"));
        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres", MultiTenancy = true });
        A.CallTo(() => swaggerOptions.Value).Returns(new SwaggerSettings { EnableSwagger = true });
        A.CallTo(() => tenantsService.GetTenantEdOrgsByInstancesAsync(_getOdsInstancesQuery, _getEducationOrganizationQuery, _getOdsInstanceManagesQuery, tenantName)).Returns(tenantDetailModel);

        var result = await ReadTenants.GetTenantEdOrgsByInstancesAsync(request, tenantsService, _getOdsInstancesQuery, _getEducationOrganizationQuery, _getOdsInstanceManagesQuery, memoryCache, options, swaggerOptions, tenantName);

        result.ShouldNotBeNull();
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_SkipsTenantHeaderValidation_WhenPathContainsSwaggerCaseInsensitive()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        var swaggerOptions = A.Fake<IOptions<SwaggerSettings>>();
        string tenantName = "tenant1";

        var tenantDetailModel = new TenantDetailModel()
        {
            TenantName = tenantName,
            OdsInstances = []
        };

        var request = A.Fake<HttpRequest>();
        var headers = A.Fake<IHeaderDictionary>();
        A.CallTo(() => request.Headers).Returns(headers);
        A.CallTo(() => headers["tenant"]).Returns(StringValues.Empty);
        A.CallTo(() => headers.Referer).Returns(StringValues.Empty);
        A.CallTo(() => request.Path).Returns(new PathString("/SWAGGER/index.html"));
        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres", MultiTenancy = true });
        A.CallTo(() => swaggerOptions.Value).Returns(new SwaggerSettings { EnableSwagger = true });
        A.CallTo(() => tenantsService.GetTenantEdOrgsByInstancesAsync(_getOdsInstancesQuery, _getEducationOrganizationQuery, _getOdsInstanceManagesQuery, tenantName)).Returns(tenantDetailModel);

        var result = await ReadTenants.GetTenantEdOrgsByInstancesAsync(request, tenantsService, _getOdsInstancesQuery, _getEducationOrganizationQuery, _getOdsInstanceManagesQuery, memoryCache, options, swaggerOptions, tenantName);

        result.ShouldNotBeNull();
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_SkipsTenantHeaderValidation_WhenRefererContainsSwaggerCaseInsensitive()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        var swaggerOptions = A.Fake<IOptions<SwaggerSettings>>();
        string tenantName = "tenant1";

        var tenantDetailModel = new TenantDetailModel()
        {
            TenantName = tenantName,
            OdsInstances = []
        };

        var request = A.Fake<HttpRequest>();
        var headers = A.Fake<IHeaderDictionary>();
        A.CallTo(() => request.Headers).Returns(headers);
        A.CallTo(() => headers["tenant"]).Returns(StringValues.Empty);
        A.CallTo(() => headers.Referer).Returns(new StringValues("https://localhost/SWAGGER/index.html"));
        A.CallTo(() => request.Path).Returns(new PathString("/tenants/tenant1/OdsInstances/edOrgs"));
        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres", MultiTenancy = true });
        A.CallTo(() => swaggerOptions.Value).Returns(new SwaggerSettings { EnableSwagger = true });
        A.CallTo(() => tenantsService.GetTenantEdOrgsByInstancesAsync(_getOdsInstancesQuery, _getEducationOrganizationQuery, _getOdsInstanceManagesQuery, tenantName)).Returns(tenantDetailModel);

        var result = await ReadTenants.GetTenantEdOrgsByInstancesAsync(request, tenantsService, _getOdsInstancesQuery, _getEducationOrganizationQuery, _getOdsInstanceManagesQuery, memoryCache, options, swaggerOptions, tenantName);

        result.ShouldNotBeNull();
    }

    [Test]
    public void GetTenantEdOrgsByInstancesAsync_EnforcesTenantHeaderValidation_WhenNotFromSwagger()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        var swaggerOptions = A.Fake<IOptions<SwaggerSettings>>();
        string tenantName = "tenant1";

        var request = A.Fake<HttpRequest>();
        var headers = A.Fake<IHeaderDictionary>();
        A.CallTo(() => request.Headers).Returns(headers);
        A.CallTo(() => headers["tenant"]).Returns(StringValues.Empty);
        A.CallTo(() => headers.Referer).Returns(StringValues.Empty);
        A.CallTo(() => request.Path).Returns(new PathString("/tenants/tenant1/OdsInstances/edOrgs"));
        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres", MultiTenancy = true });
        A.CallTo(() => swaggerOptions.Value).Returns(new SwaggerSettings { EnableSwagger = true });

        Should.ThrowAsync<ValidationException>(async () =>
        {
            await ReadTenants.GetTenantEdOrgsByInstancesAsync(request, tenantsService, _getOdsInstancesQuery, _getEducationOrganizationQuery, _getOdsInstanceManagesQuery, memoryCache, options, swaggerOptions, tenantName);
        });
    }

    [Test]
    public async Task GetTenantEdOrgsByInstancesAsync_SkipsTenantHeaderValidation_WhenRequestPathIsNull()
    {
        var tenantsService = A.Fake<ITenantsService>();
        var memoryCache = A.Fake<IMemoryCache>();
        var options = A.Fake<IOptions<AppSettings>>();
        var swaggerOptions = A.Fake<IOptions<SwaggerSettings>>();
        string tenantName = "tenant1";

        var tenantDetailModel = new TenantDetailModel()
        {
            TenantName = tenantName,
            OdsInstances = []
        };

        var request = A.Fake<HttpRequest>();
        var headers = A.Fake<IHeaderDictionary>();
        A.CallTo(() => request.Headers).Returns(headers);
        A.CallTo(() => headers["tenant"]).Returns(StringValues.Empty);
        A.CallTo(() => headers.Referer).Returns(new StringValues("https://localhost/swagger/index.html"));
        A.CallTo(() => request.Path).Returns(new PathString());
        A.CallTo(() => options.Value).Returns(new AppSettings { DatabaseEngine = "Postgres", MultiTenancy = true });
        A.CallTo(() => swaggerOptions.Value).Returns(new SwaggerSettings { EnableSwagger = true });
        A.CallTo(() => tenantsService.GetTenantEdOrgsByInstancesAsync(_getOdsInstancesQuery, _getEducationOrganizationQuery, _getOdsInstanceManagesQuery, tenantName)).Returns(tenantDetailModel);

        var result = await ReadTenants.GetTenantEdOrgsByInstancesAsync(request, tenantsService, _getOdsInstancesQuery, _getEducationOrganizationQuery, _getOdsInstanceManagesQuery, memoryCache, options, swaggerOptions, tenantName);

        result.ShouldNotBeNull();
    }
}
