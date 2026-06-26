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
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

#nullable enable

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class EditVendorCommandTests
{
    [Test]
    public void Execute_WithUnknownVendor_ThrowsNotFoundException()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"EditVendorCommand_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var command = new EditVendorCommand(usersContext);
        var model = new EditVendorModelStub
        {
            Id = 999,
            Company = "Updated Vendor",
            NamespacePrefixes = "https://new.org/ns",
            ContactName = "Updated Contact",
            ContactEmailAddress = "updated@acme.org"
        };

        Should.Throw<NotFoundException<int>>(() => command.Execute(model));
    }

    [Test]
    public void Execute_WithValidVendor_UpdatesVendorAndPersists()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"EditVendorCommand_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var vendor = new Vendor
        {
            VendorName = "Original Vendor",
            VendorNamespacePrefixes =
            [
                new VendorNamespacePrefix { NamespacePrefix = "https://original.org/ns" }
            ],
            Users =
            [
                new User { FullName = "Original Contact", Email = "original@acme.org" }
            ]
        };
        usersContext.Vendors.Add(vendor);
        usersContext.SaveChanges();

        var command = new EditVendorCommand(usersContext);
        var model = new EditVendorModelStub
        {
            Id = vendor.VendorId,
            Company = "Updated Vendor",
            NamespacePrefixes = "https://updated.org/ns",
            ContactName = "Updated Contact",
            ContactEmailAddress = "updated@acme.org"
        };

        command.Execute(model);

        var persisted = usersContext.Vendors
            .Include(v => v.VendorNamespacePrefixes)
            .Include(v => v.Users)
            .Single(v => v.VendorId == vendor.VendorId);

        persisted.VendorName.ShouldBe("Updated Vendor");
        persisted.VendorNamespacePrefixes.Single().NamespacePrefix.ShouldBe("https://updated.org/ns");
        persisted.Users.Single().FullName.ShouldBe("Updated Contact");
        persisted.Users.Single().Email.ShouldBe("updated@acme.org");
    }

    [Test]
    public void Execute_WithSystemReservedVendor_ThrowsArgumentException()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"EditVendorCommand_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var vendor = new Vendor
        {
            VendorName = VendorExtensions.ReservedNames[0]
        };
        usersContext.Vendors.Add(vendor);
        usersContext.SaveChanges();

        var command = new EditVendorCommand(usersContext);
        var model = new EditVendorModelStub
        {
            Id = vendor.VendorId,
            Company = "Trying to change reserved vendor",
            NamespacePrefixes = "https://new.org/ns",
            ContactName = "Someone",
            ContactEmailAddress = "someone@example.org"
        };

        var ex = Should.Throw<ArgumentException>(() => command.Execute(model));
        ex.Message.ShouldContain("may not be modified");
    }

    [Test]
    public void Execute_WithVendorHavingNoUsers_CreatesNewUser()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"EditVendorCommand_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var vendor = new Vendor
        {
            VendorName = "No User Vendor",
            Users = []
        };
        usersContext.Vendors.Add(vendor);
        usersContext.SaveChanges();

        var command = new EditVendorCommand(usersContext);
        var model = new EditVendorModelStub
        {
            Id = vendor.VendorId,
            Company = "No User Vendor",
            NamespacePrefixes = "https://example.org/ns",
            ContactName = "New Contact",
            ContactEmailAddress = "newcontact@example.org"
        };

        command.Execute(model);

        var persisted = usersContext.Vendors
            .Include(v => v.Users)
            .Single(v => v.VendorId == vendor.VendorId);

        persisted.Users.Count.ShouldBe(1);
        persisted.Users.Single().FullName.ShouldBe("New Contact");
        persisted.Users.Single().Email.ShouldBe("newcontact@example.org");
    }

    [Test]
    public void Execute_WithDifferentNamespacePrefix_ReplacesOldPrefix()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"EditVendorCommand_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var vendor = new Vendor
        {
            VendorName = "Namespace Test Vendor",
            VendorNamespacePrefixes =
            [
                new VendorNamespacePrefix { NamespacePrefix = "https://old.org/ns" }
            ],
            Users =
            [
                new User { FullName = "Contact", Email = "contact@example.org" }
            ]
        };
        usersContext.Vendors.Add(vendor);
        usersContext.SaveChanges();

        var command = new EditVendorCommand(usersContext);
        var model = new EditVendorModelStub
        {
            Id = vendor.VendorId,
            Company = "Namespace Test Vendor",
            NamespacePrefixes = "https://new.org/ns",
            ContactName = "Contact",
            ContactEmailAddress = "contact@example.org"
        };

        command.Execute(model);

        var persisted = usersContext.Vendors
            .Include(v => v.VendorNamespacePrefixes)
            .Single(v => v.VendorId == vendor.VendorId);

        persisted.VendorNamespacePrefixes.Count.ShouldBe(1);
        persisted.VendorNamespacePrefixes.Single().NamespacePrefix.ShouldBe("https://new.org/ns");
        persisted.VendorNamespacePrefixes.Any(p => p.NamespacePrefix == "https://old.org/ns").ShouldBeFalse();
    }

    [Test]
    public void Execute_WithNullNamespacePrefixes_ProducesEmptyNamespaceCollection()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"EditVendorCommand_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var vendor = new Vendor
        {
            VendorName = "Namespace Test Vendor",
            VendorNamespacePrefixes =
            [
                new VendorNamespacePrefix { NamespacePrefix = "https://old.org/ns" }
            ],
            Users =
            [
                new User { FullName = "Contact", Email = "contact@example.org" }
            ]
        };
        usersContext.Vendors.Add(vendor);
        usersContext.SaveChanges();

        var command = new EditVendorCommand(usersContext);
        var model = new EditVendorModelStub
        {
            Id = vendor.VendorId,
            Company = "Namespace Test Vendor",
            NamespacePrefixes = null,
            ContactName = "Contact",
            ContactEmailAddress = "contact@example.org"
        };

        command.Execute(model);

        var persisted = usersContext.Vendors
            .Include(v => v.VendorNamespacePrefixes)
            .Single(v => v.VendorId == vendor.VendorId);

        persisted.VendorNamespacePrefixes.ShouldBeEmpty();
    }

    private sealed class EditVendorModelStub : IEditVendor
    {
        public int Id { get; set; }
        public string? Company { get; set; }
        public string? NamespacePrefixes { get; set; }
        public string? ContactName { get; set; }
        public string? ContactEmailAddress { get; set; }
    }
}

#nullable restore
