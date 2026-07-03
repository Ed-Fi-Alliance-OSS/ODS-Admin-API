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
[TestFixture] public class ReadApplicationsByVendorTests {
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"ReadAppsByVendor_{Guid.NewGuid()}").Options);
    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });
    [Test] public async Task GetVendorApplications_ReturnsOkWithList() {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        ctx.Applications.Add(new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" });
        ctx.SaveChanges();
        var fakeGetOdsIds = A.Fake<IGetOdsInstanceIdsByApplicationIdQuery>();
        A.CallTo(() => fakeGetOdsIds.Execute(A<IEnumerable<int>>._)).Returns(new Dictionary<int, IList<int>>());
        var query = new GetApplicationsByVendorIdQuery(ctx);
        var result = await ReadApplicationsByVendor.GetVendorApplications(query, fakeGetOdsIds, vendor.VendorId);
        result.ShouldNotBeNull();
    }
}
