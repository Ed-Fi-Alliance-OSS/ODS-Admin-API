// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetAllClaimSetsQueryTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"GetAllClaimSets_{Guid.NewGuid()}")
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

        var result = new GetAllClaimSetsQuery(ctx, DefaultOptions()).Execute();

        result.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Test]
    public void Execute_WithNameFilter_ReturnsMatchingClaimSet()
    {
        using var ctx = CreateContext();
        ctx.ClaimSets.Add(new ClaimSet { ClaimSetName = "CS1", IsEdfiPreset = false, ForApplicationUseOnly = false });
        ctx.ClaimSets.Add(new ClaimSet { ClaimSetName = "CS2", IsEdfiPreset = false, ForApplicationUseOnly = false });
        ctx.SaveChanges();

        var result = new GetAllClaimSetsQuery(ctx, DefaultOptions())
            .Execute(new CommonQueryParams(0, 25), null, "CS1");

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("CS1");
    }

    [Test]
    public void Execute_EdfiPresetClaimSet_IsEditableFalse()
    {
        using var ctx = CreateContext();
        ctx.ClaimSets.Add(new ClaimSet { ClaimSetName = "Preset", IsEdfiPreset = true, ForApplicationUseOnly = false });
        ctx.SaveChanges();

        var result = new GetAllClaimSetsQuery(ctx, DefaultOptions()).Execute();

        result.Single(x => x.Name == "Preset").IsEditable.ShouldBeFalse();
    }

    [Test]
    public void Execute_WithIdFilter_ReturnsSingleClaimSet()
    {
        using var ctx = CreateContext();
        var cs = new ClaimSet { ClaimSetName = "FilterById", IsEdfiPreset = false, ForApplicationUseOnly = false };
        ctx.ClaimSets.Add(cs);
        ctx.SaveChanges();

        var result = new GetAllClaimSetsQuery(ctx, DefaultOptions())
            .Execute(new CommonQueryParams(0, 25), cs.ClaimSetId, null);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(cs.ClaimSetId);
    }
}
