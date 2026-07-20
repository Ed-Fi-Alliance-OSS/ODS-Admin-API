// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.
#nullable enable
using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class EditProfileCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"EditProfileV3_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_UpdatesProfileName()
    {
        using var ctx = CreateContext();
        var profile = new Profile { ProfileName = "OldProfile" };
        ctx.Profiles.Add(profile);
        ctx.SaveChanges();
        new EditProfileCommand(ctx).Execute(new EditProfileModelStub { Id = profile.ProfileId, Name = "NewProfile", Definition = "<profile/>" });
        ctx.Profiles.Single().ProfileName.ShouldBe("NewProfile");
    }

    [Test]
    public void Execute_WhenNotFound_ThrowsNotFoundException()
    {
        using var ctx = CreateContext();
        Should.Throw<NotFoundException<int>>(() => new EditProfileCommand(ctx).Execute(new EditProfileModelStub { Id = 9999, Name = "X", Definition = "<x/>" }));
    }

    private class EditProfileModelStub : IEditProfileModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Definition { get; set; }
    }
}
