// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetProfileByIdQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetProfileByIdQueryTests_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_WithExistingId_ReturnsProfile()
    {
        using var context = CreateContext();
        var profile = new Profile { ProfileName = "Assessment", ProfileDefinition = "<Profile />" };
        context.Profiles.Add(profile);
        context.SaveChanges();

        var query = new GetProfileByIdQuery(context);

        var result = query.Execute(profile.ProfileId);

        result.ProfileName.ShouldBe("Assessment");
    }

    [Test]
    public void Execute_WithUnknownId_ThrowsNotFoundException()
    {
        using var context = CreateContext();
        var query = new GetProfileByIdQuery(context);

        Should.Throw<NotFoundException<int>>(() => query.Execute(999));
    }
}
