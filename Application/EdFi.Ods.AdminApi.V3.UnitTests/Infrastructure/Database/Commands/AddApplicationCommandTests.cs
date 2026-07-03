// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
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
public class AddApplicationCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddAppCmdV3_{Guid.NewGuid()}")
            .Options);
    private static IOptions<AppSettings> Options(bool preventDuplicates = false) =>
        Microsoft.Extensions.Options.Options.Create(new AppSettings
        {
            DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25,
            PreventDuplicateApplications = preventDuplicates
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
    public void Execute_WhenPreventDuplicatesAndDuplicateExists_ThrowsAdminApiException()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        ctx.SaveChanges();
        var model = new AddApplicationModelStub { ApplicationName = "MyApp", VendorId = vendor.VendorId, ClaimSetName = "CS" };
        new AddApplicationCommand(ctx).Execute(model, Options());
        Should.Throw<AdminApiException>(() => new AddApplicationCommand(ctx).Execute(model, Options(preventDuplicates: true)));
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
