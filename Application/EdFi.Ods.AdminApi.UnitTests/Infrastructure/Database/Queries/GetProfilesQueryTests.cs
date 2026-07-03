// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetProfilesQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetProfilesQueryTests_{Guid.NewGuid()}")
            .Options);

    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });

    [Test]
    public void Execute_ReturnsAllProfilesOrderedByName()
    {
        using var context = CreateContext();
        context.Profiles.AddRange(
            new Profile { ProfileName = "Zeta", ProfileDefinition = "<Profile />" },
            new Profile { ProfileName = "Alpha", ProfileDefinition = "<Profile />" });
        context.SaveChanges();

        var query = new GetProfilesQuery(context, DefaultOptions());

        var result = query.Execute();

        result.Select(x => x.ProfileName).ShouldBe(["Alpha", "Zeta"]);
    }

    [Test]
    public void Execute_WithNameFilter_ReturnsMatchingProfile()
    {
        using var context = CreateContext();
        context.Profiles.AddRange(
            new Profile { ProfileName = "Assessment", ProfileDefinition = "<Profile />" },
            new Profile { ProfileName = "Enrollment", ProfileDefinition = "<Profile />" });
        context.SaveChanges();

        var query = new GetProfilesQuery(context, DefaultOptions());

        var result = query.Execute(new CommonQueryParams(0, 25), null, "Enrollment");

        result.Count.ShouldBe(1);
        result.Single().ProfileName.ShouldBe("Enrollment");
    }
}
