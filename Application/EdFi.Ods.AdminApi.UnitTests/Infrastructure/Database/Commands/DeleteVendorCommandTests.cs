// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class DeleteVendorCommandTests
{
    [Test]
    public void Execute_WithUnknownVendor_ThrowsNotFoundException()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"DeleteVendorCommand_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var deleteApplicationCommand = A.Fake<IDeleteApplicationCommand>();
        var command = new DeleteVendorCommand(usersContext, deleteApplicationCommand);

        Should.Throw<NotFoundException<int>>(() => command.Execute(999));
    }

    [Test]
    public void Execute_WithExistingVendor_RemovesVendorAndUsers()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"DeleteVendorCommand_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var vendor = new Vendor
        {
            VendorName = "Acme Vendor",
            Users =
            [
                new User { FullName = "Alice", Email = "alice@acme.org" }
            ]
        };
        usersContext.Vendors.Add(vendor);
        usersContext.SaveChanges();

        var deleteApplicationCommand = A.Fake<IDeleteApplicationCommand>();
        var command = new DeleteVendorCommand(usersContext, deleteApplicationCommand);

        command.Execute(vendor.VendorId);

        usersContext.Vendors.Any(v => v.VendorId == vendor.VendorId).ShouldBeFalse();
        usersContext.Users.Any().ShouldBeFalse();
    }
}
