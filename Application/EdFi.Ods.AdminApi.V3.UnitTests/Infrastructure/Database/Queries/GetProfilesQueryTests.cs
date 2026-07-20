// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetProfilesQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetProfilesV3_{Guid.NewGuid()}")
            .Options);
    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });

    [Test]
    public void Execute_ReturnsAllProfiles()
    {
        using var ctx = CreateContext();
        ctx.Profiles.Add(new Profile { ProfileName = "P1" });
        ctx.Profiles.Add(new Profile { ProfileName = "P2" });
        ctx.SaveChanges();
        new GetProfilesQuery(ctx, DefaultOptions()).Execute().Count.ShouldBe(2);
    }

    [Test]
    public void Execute_WithNameFilter_ReturnsMatch()
    {
        using var ctx = CreateContext();
        ctx.Profiles.Add(new Profile { ProfileName = "P1" });
        ctx.Profiles.Add(new Profile { ProfileName = "P2" });
        ctx.SaveChanges();
        var result = new GetProfilesQuery(ctx, DefaultOptions()).Execute(new CommonQueryParams(0, 25), null, "P1");
        result.Count.ShouldBe(1);
        result[0].ProfileName.ShouldBe("P1");
    }
}
