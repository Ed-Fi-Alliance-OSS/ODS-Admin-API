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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetAllClaimSetsQueryTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"GetAllClaimSetsV3_{Guid.NewGuid()}")
            .Options);
    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });

    [Test]
    public void Execute_ReturnsAllClaimSets()
    {
        using var ctx = CreateContext();
        ctx.ClaimSets.Add(new ClaimSet { ClaimSetName = "CS1", IsEdfiPreset = false, ForApplicationUseOnly = false });
        ctx.ClaimSets.Add(new ClaimSet { ClaimSetName = "CS2", IsEdfiPreset = false, ForApplicationUseOnly = false });
        ctx.SaveChanges();
        new GetAllClaimSetsQuery(ctx, DefaultOptions()).Execute().Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Test]
    public void Execute_WithNameFilter_ReturnsMatch()
    {
        using var ctx = CreateContext();
        ctx.ClaimSets.Add(new ClaimSet { ClaimSetName = "CS1", IsEdfiPreset = false, ForApplicationUseOnly = false });
        ctx.SaveChanges();
        var result = new GetAllClaimSetsQuery(ctx, DefaultOptions()).Execute(new CommonQueryParams(0, 25), null, "CS1");
        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("CS1");
    }
}
