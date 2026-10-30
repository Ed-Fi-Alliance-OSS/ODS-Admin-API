// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class AddApplicationCommandTests
{
    private static AdminApiSqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<AdminApiSqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddAppCmdV3_{Guid.NewGuid()}")
            .Options);
    private static IOptions<AppSettings> Options() =>
        Microsoft.Extensions.Options.Options.Create(new AppSettings
        {
            DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25
        });

    [Test]
    public void Execute_WithValidModel_PersistsApplicationAndApiClient()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        ctx.SaveChanges();
        var result = new AddApplicationCommand(ctx).Execute(new AddApplicationModelStub
        {
            ApplicationName = "MyApp", VendorId = vendor.VendorId, ClaimSetName = "CS"
        }, Options());
        result.ApplicationId.ShouldBeGreaterThan(0);
        result.Key.ShouldNotBeNullOrEmpty();
        ctx.Applications.Count().ShouldBe(1);
    }

    [Test]
    public void Execute_TrimsApplicationNameBeforePersisting()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        ctx.SaveChanges();
        var result = new AddApplicationCommand(ctx).Execute(new AddApplicationModelStub
        {
            ApplicationName = " MyApp ", VendorId = vendor.VendorId, ClaimSetName = "CS"
        }, Options());
        ctx.Applications.Single(a => a.ApplicationId == result.ApplicationId).ApplicationName.ShouldBe("MyApp");
    }

    [Test]
    public void Execute_CreateDefaultCredentialsFalse_CreatesApplicationAndEdOrgsWithoutClient()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        var ods = new OdsInstance { Name = "DS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.Vendors.Add(vendor);
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();

        var result = new AddApplicationCommand(ctx).Execute(new AddApplicationModelStub
        {
            ApplicationName = "MyApp",
            VendorId = vendor.VendorId,
            ClaimSetName = "CS",
            EducationOrganizationIds = new long[] { 1, 2 },
            CreateDefaultCredentials = false
        }, Options());

        result.ApplicationId.ShouldBeGreaterThan(0);
        result.Key.ShouldBeNull();
        result.Secret.ShouldBeNull();
        ctx.ApiClients.Count().ShouldBe(0);
        ctx.ApiClientOdsInstances.Count().ShouldBe(0);
        var application = ctx.Applications.Include(a => a.ApplicationEducationOrganizations)
            .Single(a => a.ApplicationId == result.ApplicationId);
        application.ApplicationEducationOrganizations.Select(e => e.EducationOrganizationId)
            .OrderBy(i => i).ShouldBe(new long[] { 1, 2 });
    }

    [Test]
    public void Execute_CreateDefaultCredentialsOmitted_DefaultClientInherits()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        ctx.SaveChanges();

        var result = new AddApplicationCommand(ctx).Execute(new AddApplicationModelStub
        {
            ApplicationName = "MyApp",
            VendorId = vendor.VendorId,
            ClaimSetName = "CS",
            EducationOrganizationIds = new long[] { 1, 2 }
        }, Options());

        var client = ctx.ApiClients.Include(c => c.ApplicationEducationOrganizations).Single();
        ctx.GetInheritsApplicationEducationOrganizations(client).ShouldBeTrue();
        client.ApplicationEducationOrganizations.Count.ShouldBe(2);
        result.Key.ShouldNotBeNullOrEmpty();
    }

    private class AddApplicationModelStub : IAddApplicationModel
    {
        public string? ApplicationName { get; init; }
        public int VendorId { get; init; }
        public string? ClaimSetName { get; init; }
        public IEnumerable<int>? ProfileIds { get; init; }
        public IEnumerable<long>? EducationOrganizationIds { get; init; }
        public IEnumerable<int>? DataStoreIds { get; init; }
        public bool? Enabled { get; init; }
        public bool? CreateDefaultCredentials { get; init; }
    }
}
