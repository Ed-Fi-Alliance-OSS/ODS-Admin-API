// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
using System; using System.Collections.Generic; using System.Linq; using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts; using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Applications;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy; using Microsoft.EntityFrameworkCore; using Microsoft.Extensions.Options;
using NUnit.Framework; using Shouldly;
namespace EdFi.Ods.AdminApi.UnitTests.Features.Applications;
[TestFixture] public class ReadApplicationsByOdsInstanceTests {
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"ReadAppsByOdsInstance_{Guid.NewGuid()}").Options);
    [Test] public async Task GetOdsInstanceApplications_ReturnsOkWithList() {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        var client = new ApiClient(true) { Name = "C1", Application = app };
        ctx.ApiClients.Add(client);
        ctx.ApiClientOdsInstances.Add(new ApiClientOdsInstance { ApiClient = client, OdsInstance = ods });
        ctx.SaveChanges();
        var fakeGetOdsIds = A.Fake<IGetOdsInstanceIdsByApplicationIdQuery>();
        A.CallTo(() => fakeGetOdsIds.Execute(A<IEnumerable<int>>._)).Returns(new Dictionary<int, IList<int>>());
        var query = new GetApplicationsByOdsInstanceIdQuery(ctx);
        var result = await ReadApplicationsByOdsInstance.GetOdsInstanceApplications(query, fakeGetOdsIds, ods.OdsInstanceId);
        result.ShouldNotBeNull();
    }
}
