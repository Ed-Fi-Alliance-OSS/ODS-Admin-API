// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

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

    [Test]
    public void Execute_WithSystemReservedVendor_ThrowsArgumentException()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"DeleteVendorCommand_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var vendor = new Vendor
        {
            VendorName = VendorExtensions.ReservedNames[0]
        };
        usersContext.Vendors.Add(vendor);
        usersContext.SaveChanges();

        var deleteApplicationCommand = A.Fake<IDeleteApplicationCommand>();
        var command = new DeleteVendorCommand(usersContext, deleteApplicationCommand);

        Should.Throw<ArgumentException>(() => command.Execute(vendor.VendorId));
        usersContext.Vendors.Any(v => v.VendorId == vendor.VendorId).ShouldBeTrue();
    }

    [Test]
    public void Execute_WithVendorHavingApplications_InvokesDeleteApplicationCommandForEachApplication()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"DeleteVendorCommand_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var vendor = new Vendor
        {
            VendorName = "Acme Vendor"
        };
        usersContext.Vendors.Add(vendor);
        usersContext.SaveChanges();

        var application = new Application
        {
            ApplicationName = "TestApp",
            OperationalContextUri = string.Empty,
            Vendor = vendor
        };
        usersContext.Applications.Add(application);
        usersContext.SaveChanges();

        var deleteApplicationCommand = A.Fake<IDeleteApplicationCommand>();
        var command = new DeleteVendorCommand(usersContext, deleteApplicationCommand);

        command.Execute(vendor.VendorId);

        A.CallTo(() => deleteApplicationCommand.Execute(application.ApplicationId)).MustHaveHappenedOnceExactly();
        usersContext.Vendors.Any(v => v.VendorId == vendor.VendorId).ShouldBeFalse();
    }

    [Test]
    public void Execute_WithUserHavingApiClient_RemovesApiClientBeforeRemovingUser()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"DeleteVendorCommand_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var user = new User { FullName = "Alice", Email = "alice@acme.org" };
        var vendor = new Vendor
        {
            VendorName = "Acme Vendor",
            Users = [user]
        };
        usersContext.Vendors.Add(vendor);
        usersContext.SaveChanges();

        var apiClient = new ApiClient(true) { Name = "TestClient", User = user };
        usersContext.ApiClients.Add(apiClient);
        usersContext.SaveChanges();

        var deleteApplicationCommand = A.Fake<IDeleteApplicationCommand>();
        var command = new DeleteVendorCommand(usersContext, deleteApplicationCommand);

        command.Execute(vendor.VendorId);

        usersContext.Vendors.Any(v => v.VendorId == vendor.VendorId).ShouldBeFalse();
        usersContext.ApiClients.Any(c => c.ApiClientId == apiClient.ApiClientId).ShouldBeFalse();
        usersContext.Users.Any().ShouldBeFalse();
    }
}
