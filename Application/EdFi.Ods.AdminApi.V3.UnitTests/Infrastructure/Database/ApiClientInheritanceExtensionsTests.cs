// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database;

[TestFixture]
public class ApiClientInheritanceExtensionsTests
{
    private string _dbName = null!;

    [SetUp]
    public void SetUp() => _dbName = $"ApiClientInheritance_{Guid.NewGuid()}";

    private AdminApiSqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<AdminApiSqlServerUsersContext>()
            .UseInMemoryDatabase(_dbName)
            .Options);

    private static Application NewApplication(Vendor vendor) =>
        new()
        {
            ApplicationName = "App",
            ClaimSetName = "CS",
            Vendor = vendor,
            OperationalContextUri = "uri"
        };

    [Test]
    public void NewApiClient_AddedDirectly_DefaultsToInheriting()
    {
        using var ctx = CreateContext();
        var client = new ApiClient(true) { Name = "C1" };
        ctx.ApiClients.Add(client);
        ctx.SaveChanges();

        ctx.GetInheritsApplicationEducationOrganizations(client).ShouldBeTrue();
    }

    [Test]
    public void NewApiClient_AddedThroughApplicationGraph_DefaultsToInheriting()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        var app = NewApplication(vendor);
        var client = new ApiClient(true) { Name = "C1" };
        app.ApiClients.Add(client);
        ctx.Applications.Add(app);
        ctx.SaveChanges();

        ctx.GetInheritsApplicationEducationOrganizations(client).ShouldBeTrue();
    }

    [Test]
    public void SetFalse_ThenSave_ReadsFalseFromFreshContext()
    {
        int id;
        using (var ctx = CreateContext())
        {
            var client = new ApiClient(true) { Name = "C1" };
            ctx.ApiClients.Add(client);
            ctx.SaveChanges();
            ctx.SetInheritsApplicationEducationOrganizations(client, false);
            ctx.SaveChanges();
            id = client.ApiClientId;
        }

        using var fresh = CreateContext();
        var reloaded = fresh.ApiClients.Find(id)!;
        fresh.GetInheritsApplicationEducationOrganizations(reloaded).ShouldBeFalse();
    }

    [Test]
    public void BulkGet_ReturnsFlagPerClientId()
    {
        using var ctx = CreateContext();
        var inheriting = new ApiClient(true) { Name = "C1" };
        var narrowed = new ApiClient(true) { Name = "C2" };
        ctx.ApiClients.AddRange(inheriting, narrowed);
        ctx.SaveChanges();
        ctx.SetInheritsApplicationEducationOrganizations(narrowed, false);
        ctx.SaveChanges();

        var result = ctx.GetInheritsApplicationEducationOrganizations(
            new List<int> { inheriting.ApiClientId, narrowed.ApiClientId });

        result.Count.ShouldBe(2);
        result[inheriting.ApiClientId].ShouldBeTrue();
        result[narrowed.ApiClientId].ShouldBeFalse();
    }

    [Test]
    public void Get_OnContextWithoutShadowProperty_ThrowsInvalidOperationException()
    {
        using var ctx = new SqlServerUsersContext(
            new DbContextOptionsBuilder<SqlServerUsersContext>()
                .UseInMemoryDatabase(_dbName)
                .Options);
        var client = new ApiClient(true) { Name = "C1" };
        ctx.ApiClients.Add(client);
        ctx.SaveChanges();

        var ex = Should.Throw<InvalidOperationException>(
            () => ctx.GetInheritsApplicationEducationOrganizations(client));
        ex.Message.ShouldContain(nameof(AdminApiSqlServerUsersContext));
    }
}
