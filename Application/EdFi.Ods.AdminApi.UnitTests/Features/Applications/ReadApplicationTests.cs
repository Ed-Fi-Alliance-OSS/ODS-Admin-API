// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Features.Applications;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Applications;

[TestFixture]
public class ReadApplicationTests
{
    [Test]
    public async Task GetApplications_ReturnsOkWithApplicationList()
    {
        var fakeGetAll = A.Fake<IGetAllApplicationsQuery>();
        var fakeGetOdsIds = A.Fake<IGetOdsInstanceIdsByApplicationIdQuery>();
        var vendor = new Vendor { VendorId = 1, VendorName = "V1" };
        var apps = new List<Application>
        {
            new Application { ApplicationId = 1, ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri", ApiClients = new List<ApiClient>() }
        };
        A.CallTo(() => fakeGetAll.Execute(A<CommonQueryParams>._, null, null, null, null)).Returns(apps);
        A.CallTo(() => fakeGetOdsIds.Execute(A<IEnumerable<int>>._)).Returns(new Dictionary<int, IList<int>>());

        var validator = new ReadApplication.Validator();
        var result = await ReadApplication.GetApplications(fakeGetAll, fakeGetOdsIds, validator, new CommonQueryParams(0, 25), null, null, null, null);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<ApplicationModel>>>();
    }

    [Test]
    public void Validator_WithValidIds_Passes()
    {
        var validator = new ReadApplication.Validator();
        var result = validator.Validate("1,2,3");
        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void Validator_WithNonIntegerIds_Fails()
    {
        var validator = new ReadApplication.Validator();
        var result = validator.Validate("1,abc,3");
        result.IsValid.ShouldBeFalse();
    }
}
