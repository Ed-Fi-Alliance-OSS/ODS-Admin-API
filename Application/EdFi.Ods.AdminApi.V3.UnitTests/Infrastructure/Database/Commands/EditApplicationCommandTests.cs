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
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class EditApplicationCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"EditAppCmdV3_{Guid.NewGuid()}")
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


    [Test]
    public void Execute_WithTwoApiClients_DoesNotThrowAndUpdatesApplication()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "OldName", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.ApiClients.Add(new ApiClient(true) { Name = "cred-a", Application = app });
        ctx.ApiClients.Add(new ApiClient(true) { Name = "cred-b", Application = app });
        ctx.SaveChanges();

        new EditApplicationCommand(ctx).Execute(new EditApplicationModelStub
        {
            Id = app.ApplicationId, ApplicationName = "NewName", VendorId = vendor.VendorId, ClaimSetName = "CS"
        });

        ctx.Applications.Single().ApplicationName.ShouldBe("NewName");
    }

    [Test]
    public void Execute_WithTwoApiClients_LeavesEveryCredentialNameUnchanged()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "OldName", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.ApiClients.Add(new ApiClient(true) { Name = "cred-a", Application = app });
        ctx.ApiClients.Add(new ApiClient(true) { Name = "cred-b", Application = app });
        ctx.SaveChanges();

        new EditApplicationCommand(ctx).Execute(new EditApplicationModelStub
        {
            Id = app.ApplicationId, ApplicationName = "NewName", VendorId = vendor.VendorId, ClaimSetName = "CS"
        });

        ctx.ApiClients.Select(c => c.Name).OrderBy(n => n).ShouldBe(new[] { "cred-a", "cred-b" });
    }

    [Test]
    public void Execute_WithOneRenamedApiClient_LeavesCredentialNameUnchanged()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "OldName", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.ApiClients.Add(new ApiClient(true) { Name = "user-chosen-name", Application = app });
        ctx.SaveChanges();

        new EditApplicationCommand(ctx).Execute(new EditApplicationModelStub
        {
            Id = app.ApplicationId, ApplicationName = "NewName", VendorId = vendor.VendorId, ClaimSetName = "CS"
        });

        ctx.ApiClients.Single().Name.ShouldBe("user-chosen-name");
    }

    [Test]
    public void Execute_LeavesEveryCredentialsApprovalUntouched()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "OldName", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.ApiClients.Add(new ApiClient(true) { Name = "cred-a", Application = app, IsApproved = true });
        ctx.ApiClients.Add(new ApiClient(true) { Name = "cred-b", Application = app, IsApproved = false });
        ctx.SaveChanges();

        // ADMINAPI-1484: enabled is per-ApiClient only. PUT /v3/applications/{id} has no way
        // to read or apply it - use PUT /v3/apiClients/{id} to change a credential's approval.
        new EditApplicationCommand(ctx).Execute(new EditApplicationModelStub
        {
            Id = app.ApplicationId, ApplicationName = "NewName", VendorId = vendor.VendorId, ClaimSetName = "CS"
        });

        ctx.ApiClients.Single(c => c.Name == "cred-a").IsApproved.ShouldBeTrue();
        ctx.ApiClients.Single(c => c.Name == "cred-b").IsApproved.ShouldBeFalse();
    }

    [Test]
    public void Execute_WithNoApiClients_DoesNotThrowAndUpdatesApplication()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "OldName", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.SaveChanges();

        new EditApplicationCommand(ctx).Execute(new EditApplicationModelStub
        {
            Id = app.ApplicationId, ApplicationName = "NewName", VendorId = vendor.VendorId, ClaimSetName = "CS"
        });

        ctx.Applications.Single().ApplicationName.ShouldBe("NewName");
    }


    [Test]
    public void Execute_WithTwoApiClients_AttachesEveryCredentialToEveryEducationOrganization()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "OldName", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.ApiClients.Add(new ApiClient(true) { Name = "cred-a", Application = app });
        ctx.ApiClients.Add(new ApiClient(true) { Name = "cred-b", Application = app });
        ctx.SaveChanges();

        new EditApplicationCommand(ctx).Execute(new EditApplicationModelStub
        {
            Id = app.ApplicationId,
            ApplicationName = "NewName",
            VendorId = vendor.VendorId,
            ClaimSetName = "CS",
            EducationOrganizationIds = new List<long> { 255901, 255902 }
        });

        var edOrgs = ctx.ApplicationEducationOrganizations.ToList();
        edOrgs.Count.ShouldBe(2);
        edOrgs.ShouldAllBe(aeo => aeo.ApiClients.Count == 2);
    }

    [Test]
    public void Execute_WithDuplicateEducationOrganizationIds_CreatesOneRowPerDistinctId()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "OldName", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.ApiClients.Add(new ApiClient(true) { Name = "cred-a", Application = app });
        ctx.SaveChanges();

        new EditApplicationCommand(ctx).Execute(new EditApplicationModelStub
        {
            Id = app.ApplicationId,
            ApplicationName = "NewName",
            VendorId = vendor.VendorId,
            ClaimSetName = "CS",
            EducationOrganizationIds = new List<long> { 255901, 255902, 255901, 255902 }
        });

        ctx.ApplicationEducationOrganizations.Select(aeo => aeo.EducationOrganizationId)
            .OrderBy(id => id)
            .ShouldBe(new long[] { 255901, 255902 });
    }

    [Test]
    public void Execute_WithNoApiClients_StillCreatesEducationOrganizationRows()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "OldName", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.SaveChanges();

        new EditApplicationCommand(ctx).Execute(new EditApplicationModelStub
        {
            Id = app.ApplicationId,
            ApplicationName = "NewName",
            VendorId = vendor.VendorId,
            ClaimSetName = "CS",
            EducationOrganizationIds = new List<long> { 255901 }
        });

        var edOrg = ctx.ApplicationEducationOrganizations.Single();
        edOrg.EducationOrganizationId.ShouldBe(255901);
        edOrg.ApiClients.ShouldBeEmpty();
    }


    [Test]
    public void Execute_LeavesExistingDataStoreAssignmentsUntouched()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "OldName", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        var clientA = new ApiClient(true) { Name = "cred-a", Application = app };
        var clientB = new ApiClient(true) { Name = "cred-b", Application = app };
        ctx.ApiClients.Add(clientA);
        ctx.ApiClients.Add(clientB);
        var assigned = new OdsInstance { Name = "DS-assigned", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(assigned);
        ctx.SaveChanges();
        ctx.ApiClientOdsInstances.Add(new ApiClientOdsInstance { ApiClient = clientA, OdsInstance = assigned });
        ctx.SaveChanges();

        // ADMINAPI-1484: dataStoreIds is per-ApiClient only. PUT /v3/applications/{id} has no
        // way to read or apply it - use PUT /v3/apiClients/{id} to change a credential's
        // data-store assignment. clientB stays unassigned.
        new EditApplicationCommand(ctx).Execute(new EditApplicationModelStub
        {
            Id = app.ApplicationId,
            ApplicationName = "NewName",
            VendorId = vendor.VendorId,
            ClaimSetName = "CS"
        });

        var rows = ctx.ApiClientOdsInstances.ToList();
        rows.Count.ShouldBe(1);
        rows.Single().ApiClient.ApiClientId.ShouldBe(clientA.ApiClientId);
        rows.Single().OdsInstance.OdsInstanceId.ShouldBe(assigned.OdsInstanceId);
    }


    [Test]
    public void Execute_WithThreeApiClients_AttachesEveryCredentialToEveryEducationOrganization()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "OldName", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.ApiClients.Add(new ApiClient(true) { Name = "cred-a", Application = app });
        ctx.ApiClients.Add(new ApiClient(true) { Name = "cred-b", Application = app });
        ctx.ApiClients.Add(new ApiClient(true) { Name = "cred-c", Application = app });
        ctx.SaveChanges();

        new EditApplicationCommand(ctx).Execute(new EditApplicationModelStub
        {
            Id = app.ApplicationId,
            ApplicationName = "NewName",
            VendorId = vendor.VendorId,
            ClaimSetName = "CS",
            EducationOrganizationIds = new List<long> { 255901, 255902 }
        });

        var edOrgs = ctx.ApplicationEducationOrganizations.ToList();
        edOrgs.Count.ShouldBe(2);
        // Each row must own a distinct three-element list, not a shared instance.
        edOrgs.ShouldAllBe(aeo => aeo.ApiClients.Count == 3);
        edOrgs.Select(aeo => aeo.ApiClients).Distinct().Count().ShouldBe(2);
        ctx.ApiClients.Select(c => c.Name).OrderBy(n => n).ShouldBe(new[] { "cred-a", "cred-b", "cred-c" });
    }

    private class EditApplicationModelStub : IEditApplicationModel
    {
        public int Id { get; init; }
        public string ApplicationName { get; init; } = string.Empty;
        public int VendorId { get; init; }
        public string ClaimSetName { get; init; } = string.Empty;
        public IEnumerable<int> ProfileIds { get; init; } = [];
        public IEnumerable<long> EducationOrganizationIds { get; init; } = [];
    }
}
