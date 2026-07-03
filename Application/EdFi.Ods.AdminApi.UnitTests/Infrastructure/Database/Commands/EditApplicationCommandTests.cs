// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class EditApplicationCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"EditAppCmd_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_UpdatesApplicationName()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "OldName", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.ApiClients.Add(new ApiClient(true) { Name = "C1", Application = app });
        ctx.SaveChanges();
        new EditApplicationCommand(ctx).Execute(new EditApplicationModelStub
        {
            Id = app.ApplicationId, ApplicationName = "NewName", VendorId = vendor.VendorId, ClaimSetName = "CS"
        });
        ctx.Applications.Single().ApplicationName.ShouldBe("NewName");
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        ctx.SaveChanges();
        Should.Throw<NotFoundException<int>>(() =>
            new EditApplicationCommand(ctx).Execute(new EditApplicationModelStub
            {
                Id = 9999, ApplicationName = "X", VendorId = vendor.VendorId, ClaimSetName = "CS"
            }));
    }

    private class EditApplicationModelStub : IEditApplicationModel
    {
        public int Id { get; init; }
        public string ApplicationName { get; init; } = string.Empty;
        public int VendorId { get; init; }
        public string ClaimSetName { get; init; } = string.Empty;
        public IEnumerable<int> ProfileIds => [];
        public IEnumerable<long> EducationOrganizationIds => [];
        public IEnumerable<int> OdsInstanceIds => [];
        public bool? Enabled => null;
    }
}
