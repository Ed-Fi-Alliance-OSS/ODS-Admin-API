// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class EditApiClientCommandTests
{
    private static AdminApiSqlServerUsersContext CreateContext(string? databaseName = null) =>
        new(new DbContextOptionsBuilder<AdminApiSqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: databaseName ?? $"EditApiClientV3_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_UpdatesApiClientName()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        var client = new ApiClient(true) { Name = "OldName", Application = app };
        ctx.ApiClients.Add(client);
        ctx.SaveChanges();
        new EditApiClientCommand(ctx).Execute(new EditApiClientModelStub { Id = client.ApiClientId, Name = "NewName", IsApproved = true, ApplicationId = app.ApplicationId });
        ctx.ApiClients.Single().Name.ShouldBe("NewName");
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new EditApiClientCommand(ctx).Execute(new EditApiClientModelStub { Id = 9999, Name = "X", IsApproved = true, ApplicationId = 1 }));
    }

    [Test]
    public void Execute_NeitherFieldSameApplication_KeepsNarrowedList()
    {
        using var ctx = CreateContext();
        var (app, _) = SeedApplication(ctx, "A", 1, 2, 3);
        var client = SeedClient(ctx, app, "C", false, 2);

        new EditApiClientCommand(ctx).Execute(Model(client, app, "Renamed"));

        AssertScope(ctx, client.ApiClientId, false, 2);
    }

    [Test]
    public void Execute_SendsList_ReplacesLinks()
    {
        using var ctx = CreateContext();
        var (app, _) = SeedApplication(ctx, "A", 1, 2, 3);
        var client = SeedClient(ctx, app, "C", true, 1, 2, 3);

        new EditApiClientCommand(ctx).Execute(Model(client, app, "C", ids: [1, 3]));

        AssertScope(ctx, client.ApiClientId, false, 1, 3);
        ctx.ApplicationEducationOrganizations.Count().ShouldBe(3);
    }

    [Test]
    public void Execute_NarrowedSendsTrue_SwitchesToInheriting()
    {
        using var ctx = CreateContext();
        var (app, _) = SeedApplication(ctx, "A", 1, 2, 3);
        var client = SeedClient(ctx, app, "C", false, 2);

        new EditApiClientCommand(ctx).Execute(Model(client, app, "C", inherits: true));

        AssertScope(ctx, client.ApiClientId, true, 1, 2, 3);
    }

    [Test]
    public void Execute_MoveInheriting_TakesTargetApplicationRows()
    {
        using var ctx = CreateContext();
        var (app, _) = SeedApplication(ctx, "A", 1, 2);
        var (target, _) = SeedApplication(ctx, "B", 7, 8, 9);
        var client = SeedClient(ctx, app, "C", true, 1, 2);

        new EditApiClientCommand(ctx).Execute(Model(client, target, "C"));

        var saved = LoadClient(ctx, client.ApiClientId);
        saved.Application!.ApplicationId.ShouldBe(target.ApplicationId);
        saved.ApplicationEducationOrganizations.Count.ShouldBe(3);
        saved.ApplicationEducationOrganizations
            .All(r => r.Application!.ApplicationId == target.ApplicationId).ShouldBeTrue();
        ctx.GetInheritsApplicationEducationOrganizations(saved).ShouldBeTrue();
    }

    [Test]
    public void Execute_MoveNarrowedFits_RelinksToTargetRowsWithSameIds()
    {
        using var ctx = CreateContext();
        var (app, _) = SeedApplication(ctx, "A", 1, 2);
        var (target, _) = SeedApplication(ctx, "B", 1, 2, 3);
        var client = SeedClient(ctx, app, "C", false, 2);
        var oldRowId = app.ApplicationEducationOrganizations.Single(r => r.EducationOrganizationId == 2)
            .ApplicationEducationOrganizationId;

        new EditApiClientCommand(ctx).Execute(Model(client, target, "C"));

        var saved = LoadClient(ctx, client.ApiClientId);
        saved.ApplicationEducationOrganizations.Select(r => r.EducationOrganizationId).ShouldBe([2]);
        var row = saved.ApplicationEducationOrganizations.Single();
        row.Application!.ApplicationId.ShouldBe(target.ApplicationId);
        row.ApplicationEducationOrganizationId.ShouldNotBe(oldRowId);
        ctx.GetInheritsApplicationEducationOrganizations(saved).ShouldBeFalse();
        ctx.ApplicationEducationOrganizations.Count().ShouldBe(5);
    }

    [Test]
    public void Execute_MoveNarrowedDoesNotFit_ThrowsScopeConflictAndChangesNothing()
    {
        var dbName = $"EditApiClientV3_{Guid.NewGuid()}";
        using var ctx = CreateContext(dbName);
        var (app, _) = SeedApplication(ctx, "A", 1, 2);
        var (target, _) = SeedApplication(ctx, "B", 1);
        var client = SeedClient(ctx, app, "C", false, 2);

        var ex = Should.Throw<ConflictException>(
            () => new EditApiClientCommand(ctx).Execute(Model(client, target, "Changed")));
        ex.ProblemType.ShouldBe(AdminApiProblemTypes.ConflictEducationOrganizationScope);

        using var verify = CreateContext(dbName);
        var saved = LoadClient(verify, client.ApiClientId);
        saved.Name.ShouldBe("C");
        saved.Application!.ApplicationId.ShouldBe(app.ApplicationId);
        saved.ApplicationEducationOrganizations.Select(r => r.EducationOrganizationId).ShouldBe([2]);
    }

    [Test]
    public void Execute_Move_SetsUserFromTargetVendor()
    {
        using var ctx = CreateContext();
        var (app, _) = SeedApplication(ctx, "A", 1);
        var (target, targetUser) = SeedApplication(ctx, "B", 1);
        var client = SeedClient(ctx, app, "C", true, 1);

        new EditApiClientCommand(ctx).Execute(Model(client, target, "C"));

        LoadClient(ctx, client.ApiClientId).User!.UserId.ShouldBe(targetUser.UserId);
    }

    [Test]
    public void Execute_DuplicateNameInTargetApplication_ThrowsConflict()
    {
        using var ctx = CreateContext();
        var (app, _) = SeedApplication(ctx, "A", 1);
        var (target, _) = SeedApplication(ctx, "B", 1);
        var client = SeedClient(ctx, app, "C", true, 1);
        SeedClient(ctx, target, "Taken", true, 1);

        var ex = Should.Throw<ConflictException>(() => new EditApiClientCommand(ctx).Execute(Model(client, target, "Taken")));
        ex.ProblemType.ShouldBe(AdminApiProblemTypes.Conflict);
    }

    [Test]
    public void Execute_UnchangedNameWithLegacyDuplicate_DoesNotThrow()
    {
        using var ctx = CreateContext();
        var (app, _) = SeedApplication(ctx, "A", 1);
        var client = SeedClient(ctx, app, "Dup", true, 1);
        SeedClient(ctx, app, "Dup", true, 1);

        var model = new EditApiClientModelStub
        {
            Id = client.ApiClientId,
            Name = "Dup",
            IsApproved = false,
            ApplicationId = app.ApplicationId,
        };

        Should.NotThrow(() => new EditApiClientCommand(ctx).Execute(model));
        LoadClient(ctx, client.ApiClientId).IsApproved.ShouldBeFalse();
    }

    [Test]
    public void Execute_RenameToExistingNameSameApplication_ThrowsConflict()
    {
        using var ctx = CreateContext();
        var (app, _) = SeedApplication(ctx, "A", 1);
        var client = SeedClient(ctx, app, "C", true, 1);
        SeedClient(ctx, app, "Taken", true, 1);

        var ex = Should.Throw<ConflictException>(() => new EditApiClientCommand(ctx).Execute(Model(client, app, "Taken")));
        ex.ProblemType.ShouldBe(AdminApiProblemTypes.Conflict);
    }

    [Test]
    public void Execute_SameNameSameClient_DoesNotThrow()
    {
        using var ctx = CreateContext();
        var (app, _) = SeedApplication(ctx, "A", 1);
        var client = SeedClient(ctx, app, "C", true, 1);

        Should.NotThrow(() => new EditApiClientCommand(ctx).Execute(Model(client, app, "C")));
    }

    [Test]
    public void Execute_MissingTargetApplication_ThrowsUnresolvedReferenceConflict()
    {
        using var ctx = CreateContext();
        var (app, _) = SeedApplication(ctx, "A", 1);
        var client = SeedClient(ctx, app, "C", true, 1);

        var ex = Should.Throw<ConflictException>(
            () => new EditApiClientCommand(ctx).Execute(
                new EditApiClientModelStub { Id = client.ApiClientId, Name = "C", ApplicationId = 9999 }));
        ex.ProblemType.ShouldBe(AdminApiProblemTypes.ConflictUnresolvedReference);
    }

    [Test]
    public void Execute_ClientWithoutApplication_IsTreatedAsMove()
    {
        using var ctx = CreateContext();
        var (target, _) = SeedApplication(ctx, "B", 7, 8);
        var client = new ApiClient(true) { Name = "Orphan" };
        ctx.ApiClients.Add(client);
        ctx.SaveChanges();

        new EditApiClientCommand(ctx).Execute(Model(client, target, "Orphan"));

        var saved = LoadClient(ctx, client.ApiClientId);
        saved.Application!.ApplicationId.ShouldBe(target.ApplicationId);
        AssertScope(ctx, client.ApiClientId, true, 7, 8);
    }

    [Test]
    public void Execute_RenameWithSurroundingWhitespaceToExistingName_ThrowsConflict()
    {
        using var ctx = CreateContext();
        var (app, _) = SeedApplication(ctx, "A", 1);
        var client = SeedClient(ctx, app, "C", true, 1);
        SeedClient(ctx, app, "Taken", true, 1);

        var ex = Should.Throw<ConflictException>(
            () => new EditApiClientCommand(ctx).Execute(Model(client, app, " Taken ")));
        ex.ProblemType.ShouldBe(AdminApiProblemTypes.Conflict);
    }

    [Test]
    public void Execute_RenameTwiceWithWhitespace_SecondThrowsConflict()
    {
        using var ctx = CreateContext();
        var (app, _) = SeedApplication(ctx, "A", 1);
        var clientA = SeedClient(ctx, app, "A", true, 1);
        var clientB = SeedClient(ctx, app, "B", true, 1);

        new EditApiClientCommand(ctx).Execute(Model(clientA, app, " Taken "));
        LoadClient(ctx, clientA.ApiClientId).Name.ShouldBe("Taken");

        var ex = Should.Throw<ConflictException>(
            () => new EditApiClientCommand(ctx).Execute(Model(clientB, app, " Taken ")));
        ex.ProblemType.ShouldBe(AdminApiProblemTypes.Conflict);
    }

    [Test]
    public void Execute_SavesTrimmedName()
    {
        using var ctx = CreateContext();
        var (app, _) = SeedApplication(ctx, "A", 1);
        var client = SeedClient(ctx, app, "C", true, 1);

        new EditApiClientCommand(ctx).Execute(Model(client, app, "  Renamed  "));

        LoadClient(ctx, client.ApiClientId).Name.ShouldBe("Renamed");
    }

    private static (Application App, User User) SeedApplication(
        AdminApiSqlServerUsersContext ctx, string name, params long[] edOrgIds)
    {
        var vendor = new Vendor { VendorName = $"Vendor {name}" };
        var user = new User { FullName = $"User {name}", Email = $"{name}@x.com", Vendor = vendor };
        var app = new Application
        {
            ApplicationName = $"App {name}",
            ClaimSetName = "CS",
            Vendor = vendor,
            OperationalContextUri = "uri",
        };

        foreach (var id in edOrgIds)
        {
            app.ApplicationEducationOrganizations.Add(
                new ApplicationEducationOrganization { EducationOrganizationId = id, Application = app });
        }

        ctx.Vendors.Add(vendor);
        ctx.Users.Add(user);
        ctx.Applications.Add(app);
        ctx.SaveChanges();
        return (app, user);
    }

    private static ApiClient SeedClient(
        AdminApiSqlServerUsersContext ctx, Application app, string name, bool inherits, params long[] edOrgIds)
    {
        var client = new ApiClient(true) { Name = name, Application = app };

        foreach (var row in app.ApplicationEducationOrganizations.Where(r => edOrgIds.Contains(r.EducationOrganizationId)))
        {
            client.ApplicationEducationOrganizations.Add(row);
        }

        ctx.ApiClients.Add(client);
        ctx.SetInheritsApplicationEducationOrganizations(client, inherits);
        ctx.SaveChanges();
        return client;
    }

    private static ApiClient LoadClient(AdminApiSqlServerUsersContext ctx, int id) =>
        ctx.ApiClients
            .Include(c => c.Application)
            .Include(c => c.User)
            .Include(c => c.ApplicationEducationOrganizations).ThenInclude(r => r.Application)
            .Single(c => c.ApiClientId == id);

    private static void AssertScope(AdminApiSqlServerUsersContext ctx, int id, bool inherits, params long[] edOrgIds)
    {
        var saved = LoadClient(ctx, id);
        ctx.GetInheritsApplicationEducationOrganizations(saved).ShouldBe(inherits);
        saved.ApplicationEducationOrganizations
            .Select(r => r.EducationOrganizationId).OrderBy(x => x).ToArray().ShouldBe(edOrgIds);
    }

    private static EditApiClientModelStub Model(
        ApiClient client, Application app, string name, long[]? ids = null, bool? inherits = null) =>
        new()
        {
            Id = client.ApiClientId,
            Name = name,
            IsApproved = true,
            ApplicationId = app.ApplicationId,
            EducationOrganizationIds = ids,
            InheritsApplicationEducationOrganizations = inherits,
        };

    private class EditApiClientModelStub : IEditApiClientModel
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsApproved { get; init; }
        public int ApplicationId { get; init; }
        public IEnumerable<int>? DataStoreIds => null;
        public IEnumerable<long>? EducationOrganizationIds { get; init; }
        public bool? InheritsApplicationEducationOrganizations { get; init; }
    }
}
