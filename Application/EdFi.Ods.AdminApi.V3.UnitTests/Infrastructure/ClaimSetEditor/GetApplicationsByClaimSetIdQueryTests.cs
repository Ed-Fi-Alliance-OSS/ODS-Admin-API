// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using AdminApplication = EdFi.Admin.DataAccess.Models.Application;
using AdminVendor = EdFi.Admin.DataAccess.Models.Vendor;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.ClaimSetEditor;

[TestFixture]
public class GetApplicationsByClaimSetIdQueryTests
{
    private static SqlServerSecurityContext CreateSecurityContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"AppsByCSV3_sec_{Guid.NewGuid()}")
            .Options);
    private static SqlServerUsersContext CreateUsersContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AppsByCSV3_usr_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_ReturnsMatchingApplications()
    {
        using var secCtx = CreateSecurityContext();
        using var usrCtx = CreateUsersContext();
        var cs = new ClaimSet { ClaimSetName = "TestCS", IsEdfiPreset = false, ForApplicationUseOnly = false };
        secCtx.ClaimSets.Add(cs);
        secCtx.SaveChanges();
        var vendor = new AdminVendor { VendorName = "V1" };
        usrCtx.Vendors.Add(vendor);
        usrCtx.Applications.Add(new AdminApplication { ApplicationName = "App1", ClaimSetName = "TestCS", Vendor = vendor, OperationalContextUri = "uri" });
        usrCtx.SaveChanges();
        var result = new GetApplicationsByClaimSetIdQuery(secCtx, usrCtx).Execute(cs.ClaimSetId).ToList();
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("App1");
    }

    [Test]
    public void Execute_WhenNoApps_ReturnsEmpty()
    {
        using var secCtx = CreateSecurityContext();
        using var usrCtx = CreateUsersContext();
        var cs = new ClaimSet { ClaimSetName = "EmptyCS", IsEdfiPreset = false, ForApplicationUseOnly = false };
        secCtx.ClaimSets.Add(cs);
        secCtx.SaveChanges();
        new GetApplicationsByClaimSetIdQuery(secCtx, usrCtx).Execute(cs.ClaimSetId).ShouldBeEmpty();
    }
}
