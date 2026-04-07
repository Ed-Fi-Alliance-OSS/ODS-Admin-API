// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using NUnit.Framework;
using Shouldly;
using ApplicationMapper = EdFi.Ods.AdminApi.Features.Applications.ApplicationMapper;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Applications;

[TestFixture]
public class ApplicationMapperTests
{
    // -----------------------------------------------------------------------
    // ToResult(AddApplicationResult)
    // -----------------------------------------------------------------------

    [Test]
    public void ToResult_AddApplicationResult_MapsIdKeySecret()
    {
        var source = new AddApplicationResult
        {
            ApplicationId = 10,
            Key = "abc123",
            Secret = "secret!"
        };

        var result = ApplicationMapper.ToResult(source);

        result.Id.ShouldBe(10);
        result.Key.ShouldBe("abc123");
        result.Secret.ShouldBe("secret!");
    }

    [Test]
    public void ToResult_AddApplicationResult_NullKeyAndSecret_MapsNulls()
    {
        var source = new AddApplicationResult
        {
            ApplicationId = 5,
            Key = null,
            Secret = null
        };

        var result = ApplicationMapper.ToResult(source);

        result.Id.ShouldBe(5);
        result.Key.ShouldBeNull();
        result.Secret.ShouldBeNull();
    }

    // -----------------------------------------------------------------------
    // ToResult(RegenerateApplicationApiClientSecretResult)
    // -----------------------------------------------------------------------

    [Test]
    public void ToResult_RegenerateResult_MapsApplicationIdKeySecret()
    {
        var application = new Application
        {
            ApplicationId = 7,
            ApplicationName = "App",
            OperationalContextUri = "uri://ed-fi.org"
        };
        var source = new RegenerateApplicationApiClientSecretResult
        {
            Application = application,
            Key = "newKey",
            Secret = "newSecret"
        };

        var result = ApplicationMapper.ToResult(source);

        result.Id.ShouldBe(7);
        result.Key.ShouldBe("newKey");
        result.Secret.ShouldBe("newSecret");
    }

    [Test]
    public void ToResult_RegenerateResult_NullKeyAndSecret_MapsNulls()
    {
        var application = new Application
        {
            ApplicationId = 3,
            OperationalContextUri = "uri://ed-fi.org"
        };
        var source = new RegenerateApplicationApiClientSecretResult
        {
            Application = application,
            Key = null,
            Secret = null
        };

        var result = ApplicationMapper.ToResult(source);

        result.Id.ShouldBe(3);
        result.Key.ShouldBeNull();
        result.Secret.ShouldBeNull();
    }

    // -----------------------------------------------------------------------
    // ToModel(Application, IList<int>)
    // -----------------------------------------------------------------------

    [Test]
    public void ToModel_MapsScalarProperties()
    {
        var application = new Application
        {
            ApplicationId = 42,
            ApplicationName = "Test Application",
            ClaimSetName = "SIS Vendor",
            OperationalContextUri = "uri://ed-fi.org",
            ApiClients = new List<ApiClient> { new ApiClient { IsApproved = true } },
            Profiles = new List<Profile>(),
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>()
        };
        var odsInstanceIds = new List<int> { 1, 2 };

        var model = ApplicationMapper.ToModel(application, odsInstanceIds);

        model.Id.ShouldBe(42);
        model.ApplicationName.ShouldBe("Test Application");
        model.ClaimSetName.ShouldBe("SIS Vendor");
    }

    [Test]
    public void ToModel_AllApiClientsApproved_EnabledIsTrue()
    {
        var application = new Application
        {
            ApplicationId = 1,
            OperationalContextUri = "uri://ed-fi.org",
            ApiClients = new List<ApiClient>
            {
                new ApiClient { IsApproved = true },
                new ApiClient { IsApproved = true }
            },
            Profiles = new List<Profile>(),
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>()
        };

        var model = ApplicationMapper.ToModel(application, new List<int>());

        model.Enabled.ShouldBeTrue();
    }

    [Test]
    public void ToModel_AnyApiClientNotApproved_EnabledIsFalse()
    {
        var application = new Application
        {
            ApplicationId = 1,
            OperationalContextUri = "uri://ed-fi.org",
            ApiClients = new List<ApiClient>
            {
                new ApiClient { IsApproved = true },
                new ApiClient { IsApproved = false }
            },
            Profiles = new List<Profile>(),
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>()
        };

        var model = ApplicationMapper.ToModel(application, new List<int>());

        model.Enabled.ShouldBeFalse();
    }

    [Test]
    public void ToModel_OdsInstanceIds_AreMappedDirectly()
    {
        var application = new Application
        {
            ApplicationId = 1,
            OperationalContextUri = "uri://ed-fi.org",
            ApiClients = new List<ApiClient>(),
            Profiles = new List<Profile>(),
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>()
        };
        var odsInstanceIds = new List<int> { 10, 20, 30 };

        var model = ApplicationMapper.ToModel(application, odsInstanceIds);

        model.OdsInstanceIds.ShouldBe(odsInstanceIds);
    }

    [Test]
    public void ToModel_EducationOrganizationIds_AreMappedFromEntity()
    {
        var application = new Application
        {
            ApplicationId = 1,
            OperationalContextUri = "uri://ed-fi.org",
            ApiClients = new List<ApiClient>(),
            Profiles = new List<Profile>(),
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>
            {
                new ApplicationEducationOrganization { EducationOrganizationId = 100 },
                new ApplicationEducationOrganization { EducationOrganizationId = 200 }
            }
        };

        var model = ApplicationMapper.ToModel(application, new List<int>());

        model.EducationOrganizationIds.ShouldContain(100L);
        model.EducationOrganizationIds.ShouldContain(200L);
        model.EducationOrganizationIds!.Count.ShouldBe(2);
    }

    [Test]
    public void ToModel_ProfileIds_AreMappedFromEntity()
    {
        var application = new Application
        {
            ApplicationId = 1,
            OperationalContextUri = "uri://ed-fi.org",
            ApiClients = new List<ApiClient>(),
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>(),
            Profiles = new List<Profile>
            {
                new Profile { ProfileId = 5 },
                new Profile { ProfileId = 9 }
            }
        };

        var model = ApplicationMapper.ToModel(application, new List<int>());

        model.ProfileIds.ShouldContain(5);
        model.ProfileIds.ShouldContain(9);
        model.ProfileIds!.Count.ShouldBe(2);
    }

    [Test]
    public void ToModel_VendorId_IsMappedFromVendorNavigation()
    {
        var application = new Application
        {
            ApplicationId = 1,
            OperationalContextUri = "uri://ed-fi.org",
            Vendor = new Vendor { VendorId = 77, VendorName = "Test Vendor" },
            ApiClients = new List<ApiClient>(),
            Profiles = new List<Profile>(),
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>()
        };

        var model = ApplicationMapper.ToModel(application, new List<int>());

        model.VendorId.ShouldBe(77);
    }

    [Test]
    public void ToModel_NoVendor_VendorIdIsNull()
    {
        var application = new Application
        {
            ApplicationId = 1,
            OperationalContextUri = "uri://ed-fi.org",
            Vendor = null,
            ApiClients = new List<ApiClient>(),
            Profiles = new List<Profile>(),
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>()
        };

        var model = ApplicationMapper.ToModel(application, new List<int>());

        model.VendorId.ShouldBeNull();
    }

    // -----------------------------------------------------------------------
    // ToModelList
    // -----------------------------------------------------------------------

    [Test]
    public void ToModelList_ReturnsOneModelPerApplication()
    {
        var app1 = new Application
        {
            ApplicationId = 1,
            ApplicationName = "App One",
            OperationalContextUri = "uri://ed-fi.org",
            ApiClients = new List<ApiClient>(),
            Profiles = new List<Profile>(),
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>()
        };
        var app2 = new Application
        {
            ApplicationId = 2,
            ApplicationName = "App Two",
            OperationalContextUri = "uri://ed-fi.org",
            ApiClients = new List<ApiClient>(),
            Profiles = new List<Profile>(),
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>()
        };
        var odsMap = new Dictionary<int, IList<int>>
        {
            [1] = new List<int> { 10 },
            [2] = new List<int> { 20, 30 }
        };

        var models = ApplicationMapper.ToModelList(new[] { app1, app2 }, odsMap);

        models.Count.ShouldBe(2);
        models[0].Id.ShouldBe(1);
        models[0].OdsInstanceIds.ShouldBe(new List<int> { 10 });
        models[1].Id.ShouldBe(2);
        models[1].OdsInstanceIds.ShouldBe(new List<int> { 20, 30 });
    }

    [Test]
    public void ToModelList_ApplicationWithNoOdsEntry_OdsInstanceIdsIsEmpty()
    {
        var app = new Application
        {
            ApplicationId = 99,
            OperationalContextUri = "uri://ed-fi.org",
            ApiClients = new List<ApiClient>(),
            Profiles = new List<Profile>(),
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>()
        };
        var emptyOdsMap = new Dictionary<int, IList<int>>();

        var models = ApplicationMapper.ToModelList(new[] { app }, emptyOdsMap);

        models.Count.ShouldBe(1);
        models[0].OdsInstanceIds.ShouldNotBeNull();
        models[0].OdsInstanceIds!.Count.ShouldBe(0);
    }

    [Test]
    public void ToModelList_EmptyInput_ReturnsEmptyList()
    {
        var models = ApplicationMapper.ToModelList(
            Array.Empty<Application>(),
            new Dictionary<int, IList<int>>());

        models.ShouldBeEmpty();
    }
}
