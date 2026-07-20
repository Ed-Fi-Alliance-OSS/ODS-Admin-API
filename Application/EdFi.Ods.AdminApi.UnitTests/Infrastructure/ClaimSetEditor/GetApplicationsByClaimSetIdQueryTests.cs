// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using AdminApplication = EdFi.Admin.DataAccess.Models.Application;
using AdminVendor = EdFi.Admin.DataAccess.Models.Vendor;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.ClaimSetEditor;

[TestFixture]
public class GetApplicationsByClaimSetIdQueryTests
{
    private static SqlServerSecurityContext CreateSecurityContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"AppsByClaimSet_sec_{Guid.NewGuid()}")
            .Options);

    private static SqlServerUsersContext CreateUsersContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AppsByClaimSet_usr_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_ReturnsApplicationsWithMatchingClaimSetName()
    {
        using var secCtx = CreateSecurityContext();
        using var usrCtx = CreateUsersContext();

        var cs = new ClaimSet { ClaimSetName = "TestCS", IsEdfiPreset = false, ForApplicationUseOnly = false };
        secCtx.ClaimSets.Add(cs);
        secCtx.SaveChanges();

        var vendor = new AdminVendor { VendorName = "Vendor1" };
        usrCtx.Vendors.Add(vendor);
        usrCtx.Applications.Add(new AdminApplication { ApplicationName = "App1", ClaimSetName = "TestCS", Vendor = vendor, OperationalContextUri = "uri" });
        usrCtx.Applications.Add(new AdminApplication { ApplicationName = "App2", ClaimSetName = "OtherCS", Vendor = vendor, OperationalContextUri = "uri" });
        usrCtx.SaveChanges();

        var result = new GetApplicationsByClaimSetIdQuery(secCtx, usrCtx).Execute(cs.ClaimSetId).ToList();

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("App1");
    }

    [Test]
    public void Execute_WhenNoMatchingApplications_ReturnsEmpty()
    {
        using var secCtx = CreateSecurityContext();
        using var usrCtx = CreateUsersContext();

        var cs = new ClaimSet { ClaimSetName = "EmptyCS", IsEdfiPreset = false, ForApplicationUseOnly = false };
        secCtx.ClaimSets.Add(cs);
        secCtx.SaveChanges();

        var result = new GetApplicationsByClaimSetIdQuery(secCtx, usrCtx).Execute(cs.ClaimSetId).ToList();

        result.ShouldBeEmpty();
    }

    [Test]
    public void ExecuteCount_ReturnsCorrectCount()
    {
        using var secCtx = CreateSecurityContext();
        using var usrCtx = CreateUsersContext();

        var cs = new ClaimSet { ClaimSetName = "CountCS", IsEdfiPreset = false, ForApplicationUseOnly = false };
        secCtx.ClaimSets.Add(cs);
        secCtx.SaveChanges();

        var vendor = new AdminVendor { VendorName = "V1" };
        usrCtx.Vendors.Add(vendor);
        usrCtx.Applications.Add(new AdminApplication { ApplicationName = "A1", ClaimSetName = "CountCS", Vendor = vendor, OperationalContextUri = "uri" });
        usrCtx.Applications.Add(new AdminApplication { ApplicationName = "A2", ClaimSetName = "CountCS", Vendor = vendor, OperationalContextUri = "uri" });
        usrCtx.SaveChanges();

        new GetApplicationsByClaimSetIdQuery(secCtx, usrCtx).ExecuteCount(cs.ClaimSetId).ShouldBe(2);
    }
}
