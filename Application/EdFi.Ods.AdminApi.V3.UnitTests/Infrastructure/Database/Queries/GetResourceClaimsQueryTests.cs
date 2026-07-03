// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetResourceClaimsQueryTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"GetResourceClaimsV3_{Guid.NewGuid()}")
            .Options);
    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });

    [Test]
    public void Execute_ReturnsRootClaims()
    {
        using var ctx = CreateContext();
        ctx.ResourceClaims.Add(new ResourceClaim { ResourceName = "schools", ClaimName = "schools" });
        ctx.ResourceClaims.Add(new ResourceClaim { ResourceName = "students", ClaimName = "students" });
        ctx.SaveChanges();
        var result = new GetResourceClaimsQuery(ctx, DefaultOptions()).Execute().ToList();
        result.Count.ShouldBe(2);
    }

    [Test]
    public void Execute_WithNameFilter_ReturnsSingleClaim()
    {
        using var ctx = CreateContext();
        ctx.ResourceClaims.Add(new ResourceClaim { ResourceName = "schools", ClaimName = "schools" });
        ctx.SaveChanges();
        var result = new GetResourceClaimsQuery(ctx, DefaultOptions()).Execute(new CommonQueryParams(0, 25), null, "schools").ToList();
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("schools");
    }
}
