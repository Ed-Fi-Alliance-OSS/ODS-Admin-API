// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
#nullable enable
using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class AddProfileCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddProfileV3_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_PersistsProfile()
    {
        using var ctx = CreateContext();
        var result = new AddProfileCommand(ctx).Execute(new AddProfileModelStub { Name = "Profile1", Definition = "<profile/>" });
        result.ProfileId.ShouldBeGreaterThan(0);
        result.ProfileName.ShouldBe("Profile1");
        ctx.Profiles.Count().ShouldBe(1);
    }

    private class AddProfileModelStub : IAddProfileModel
    {
        public string? Name { get; init; }
        public string? Definition { get; init; }
    }
}
