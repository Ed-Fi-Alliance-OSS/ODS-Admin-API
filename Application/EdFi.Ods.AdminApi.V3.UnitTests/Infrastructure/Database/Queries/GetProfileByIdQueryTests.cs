// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetProfileByIdQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetProfileByIdV3_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_WhenExists_ReturnsProfile()
    {
        using var ctx = CreateContext();
        var profile = new Profile { ProfileName = "Profile1" };
        ctx.Profiles.Add(profile);
        ctx.SaveChanges();
        var result = new GetProfileByIdQuery(ctx).Execute(profile.ProfileId);
        result.ShouldNotBeNull();
        result.ProfileName.ShouldBe("Profile1");
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new GetProfileByIdQuery(ctx).Execute(9999));
    }
}
