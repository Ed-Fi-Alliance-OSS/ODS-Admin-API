// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using System; using System.Collections.Generic; using System.Linq; using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Features.Applications;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy; using NUnit.Framework; using Shouldly;
namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.Applications;
[TestFixture] public class ReadApplicationsByDataStoreTests {
    [Test] public async Task GetDataStoreApplications_ReturnsOkWithList() {
        var fakeGetApps = A.Fake<IGetApplicationsByDataStoreIdQuery>();
        var fakeGetDataStoreIds = A.Fake<IGetDataStoreIdsByApplicationIdQuery>();
        var vendor = new Vendor { VendorId = 1, VendorName = "V1" };
        A.CallTo(() => fakeGetApps.Execute(1)).Returns(new List<Application> {
            new Application { ApplicationId = 1, ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri", ApiClients = new List<ApiClient>() }
        });
        A.CallTo(() => fakeGetDataStoreIds.Execute(A<IEnumerable<int>>._)).Returns(new Dictionary<int, IList<int>>());
        var result = await ReadApplicationsByDataStore.GetDataStoreApplications(fakeGetApps, fakeGetDataStoreIds, 1);
        result.ShouldNotBeNull();
    }
}
