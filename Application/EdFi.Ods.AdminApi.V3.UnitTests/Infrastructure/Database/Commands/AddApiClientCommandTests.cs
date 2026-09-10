// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class AddApiClientCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddApiClientV3_{Guid.NewGuid()}")
            .Options);
    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });

    [Test]
    public void Execute_PersistsApiClient()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.SaveChanges();
        var result = new AddApiClientCommand(ctx).Execute(new AddApiClientModelStub { Name = "Client1", IsApproved = true, ApplicationId = app.ApplicationId }, DefaultOptions());
        result.Id.ShouldBeGreaterThan(0);
        result.Key.ShouldNotBeNullOrEmpty();
        ctx.ApiClients.Count().ShouldBe(1);
    }

    [Test]
    public void Execute_TrimsNameBeforePersisting()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.SaveChanges();
        var result = new AddApiClientCommand(ctx).Execute(new AddApiClientModelStub { Name = " Client1 ", IsApproved = true, ApplicationId = app.ApplicationId }, DefaultOptions());
        ctx.ApiClients.Single(c => c.ApiClientId == result.Id).Name.ShouldBe("Client1");
    }


    [Test]
    public void Execute_GivesNewApiClientTheApplicationsEducationOrganizations()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.ApplicationEducationOrganizations.Add(new ApplicationEducationOrganization { EducationOrganizationId = 255901, Application = app });
        ctx.ApplicationEducationOrganizations.Add(new ApplicationEducationOrganization { EducationOrganizationId = 255902, Application = app });
        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();

        var result = new AddApiClientCommand(ctx).Execute(
            new AddApiClientModelStub { Name = "cred-b", IsApproved = true, ApplicationId = app.ApplicationId },
            DefaultOptions());

        ctx.ChangeTracker.Clear();
        var created = ctx.ApiClients.Include(c => c.ApplicationEducationOrganizations).Single(c => c.ApiClientId == result.Id);
        created.ApplicationEducationOrganizations.Select(aeo => aeo.EducationOrganizationId)
            .OrderBy(id => id)
            .ShouldBe(new long[] { 255901, 255902 });
    }

    [Test]
    public void Execute_DoesNotCreateAdditionalEducationOrganizationRows()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.ApplicationEducationOrganizations.Add(new ApplicationEducationOrganization { EducationOrganizationId = 255901, Application = app });
        ctx.ApplicationEducationOrganizations.Add(new ApplicationEducationOrganization { EducationOrganizationId = 255902, Application = app });
        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();

        new AddApiClientCommand(ctx).Execute(
            new AddApiClientModelStub { Name = "cred-b", IsApproved = true, ApplicationId = app.ApplicationId },
            DefaultOptions());

        ctx.ChangeTracker.Clear();
        ctx.ApplicationEducationOrganizations.Count().ShouldBe(2);
    }

    [Test]
    public void Execute_AssignsTheVendorsUserToTheNewApiClient()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        vendor.Users.Add(new User { FullName = "Vendor User", Email = "vendor.user@example.org" });
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();

        var result = new AddApiClientCommand(ctx).Execute(
            new AddApiClientModelStub { Name = "cred-b", IsApproved = true, ApplicationId = app.ApplicationId },
            DefaultOptions());

        ctx.ChangeTracker.Clear();
        var created = ctx.ApiClients.Include(c => c.User).Single(c => c.ApiClientId == result.Id);
        created.User.ShouldNotBeNull();
        created.User.FullName.ShouldBe("Vendor User");
    }


    [Test]
    public void Execute_CreatesDataStoreRowsForTheNewApiClient()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        var odsInstance = new OdsInstance { Name = "DS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(odsInstance);
        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();

        var result = new AddApiClientCommand(ctx).Execute(
            new AddApiClientModelStub
            {
                Name = "cred-b",
                IsApproved = true,
                ApplicationId = app.ApplicationId,
                DataStoreIds = new List<int> { odsInstance.OdsInstanceId }
            },
            DefaultOptions());

        ctx.ChangeTracker.Clear();
        ctx.ApiClientOdsInstances
            .Include(o => o.ApiClient)
            .Include(o => o.OdsInstance)
            .Count(o => o.ApiClient.ApiClientId == result.Id && o.OdsInstance.OdsInstanceId == odsInstance.OdsInstanceId)
            .ShouldBe(1);
    }


    [Test]
    public void Execute_WhenApplicationNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        ctx.SaveChanges();

        Should.Throw<NotFoundException<int>>(() =>
            new AddApiClientCommand(ctx).Execute(
                new AddApiClientModelStub { Name = "cred-x", IsApproved = true, ApplicationId = 9999 },
                DefaultOptions()));
    }

    [Test]
    public void Execute_WhenApplicationHasNoEducationOrganizations_CreatesApiClientWithNone()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.SaveChanges();
        ctx.ChangeTracker.Clear();

        var result = new AddApiClientCommand(ctx).Execute(
            new AddApiClientModelStub { Name = "cred-b", IsApproved = true, ApplicationId = app.ApplicationId },
            DefaultOptions());

        ctx.ChangeTracker.Clear();
        var created = ctx.ApiClients.Include(c => c.ApplicationEducationOrganizations).Single(c => c.ApiClientId == result.Id);
        created.ApplicationEducationOrganizations.ShouldBeEmpty();
        ctx.ApplicationEducationOrganizations.Count().ShouldBe(0);
    }

    private class AddApiClientModelStub : IAddApiClientModel
    {
        public string Name { get; init; } = string.Empty;
        public bool IsApproved { get; init; }
        public int ApplicationId { get; init; }
        public IEnumerable<int>? DataStoreIds { get; init; }
    }
}
