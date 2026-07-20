// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable
using System;
using System.Collections.Generic;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class ValidateApplicationExistsQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"ValidateAppV3_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_WhenNoDuplicate_ReturnsFalse()
    {
        using var ctx = CreateContext();
        var result = new ValidateApplicationExistsQuery(ctx).Execute(new AddApplicationModelStub
        {
            ApplicationName = "NonExistent", VendorId = 1, ClaimSetName = "CS"
        });
        result.ShouldBeFalse();
    }

    [Test]
    public void Execute_WhenDuplicateExists_ReturnsTrue()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application
        {
            ApplicationName = "MyApp", ClaimSetName = "CS", Vendor = vendor,
            OperationalContextUri = "uri",
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>(),
            Profiles = new List<Profile>()
        };
        ctx.Applications.Add(app);
        var client = new ApiClient(true) { Name = "C1", Application = app };
        ctx.ApiClients.Add(client);
        ctx.SaveChanges();
        var result = new ValidateApplicationExistsQuery(ctx).Execute(new AddApplicationModelStub
        {
            ApplicationName = "MyApp", VendorId = vendor.VendorId, ClaimSetName = "CS"
        });
        result.ShouldBeTrue();
    }

    private class AddApplicationModelStub : IAddApplicationModel
    {
        public string? ApplicationName { get; init; }
        public int VendorId { get; init; }
        public string? ClaimSetName { get; init; }
        public IEnumerable<int>? ProfileIds => null;
        public IEnumerable<long>? EducationOrganizationIds => null;
        public IEnumerable<int>? DataStoreIds => null;
        public bool? Enabled => null;
    }
}
