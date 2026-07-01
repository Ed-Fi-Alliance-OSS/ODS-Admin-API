// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

#nullable enable

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class AddVendorCommandTests
{
    [Test]
    public void Execute_WithValidModel_PersistsVendorNamespaceAndUser()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddVendorCommand_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var command = new AddVendorCommand(usersContext);
        var request = new AddVendorModelStub
        {
            Company = "Acme Vendor",
            NamespacePrefixes = "https://acme.org/ns",
            ContactName = "Alice",
            ContactEmailAddress = "alice@acme.org"
        };

        var vendor = command.Execute(request);

        vendor.VendorId.ShouldBeGreaterThan(0);

        var persisted = usersContext.Vendors
            .Include(v => v.VendorNamespacePrefixes)
            .Include(v => v.Users)
            .Single(v => v.VendorId == vendor.VendorId);

        persisted.VendorName.ShouldBe("Acme Vendor");
        persisted.VendorNamespacePrefixes.Single().NamespacePrefix.ShouldBe("https://acme.org/ns");
        persisted.Users.Single().FullName.ShouldBe("Alice");
        persisted.Users.Single().Email.ShouldBe("alice@acme.org");
    }

    [Test]
    public void Execute_WithNullNamespacePrefixes_PersistsVendorWithEmptyPrefixCollection()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddVendorCommand_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var command = new AddVendorCommand(usersContext);
        var request = new AddVendorModelStub
        {
            Company = "Acme Vendor",
            NamespacePrefixes = null,
            ContactName = "Alice",
            ContactEmailAddress = "alice@acme.org"
        };

        var vendor = command.Execute(request);

        vendor.VendorId.ShouldBeGreaterThan(0);

        var persisted = usersContext.Vendors
            .Include(v => v.VendorNamespacePrefixes)
            .Include(v => v.Users)
            .Single(v => v.VendorId == vendor.VendorId);

        var prefixCount = persisted.VendorNamespacePrefixes?.Count ?? 0;
        prefixCount.ShouldBe(0);
    }

    [Test]
    public void Execute_WithMultipleCommaSeparatedNamespaces_PersistsEachPrefixSeparately()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddVendorCommand_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var command = new AddVendorCommand(usersContext);
        var request = new AddVendorModelStub
        {
            Company = "Acme Vendor",
            NamespacePrefixes = "https://a.org/ns,https://b.org/ns",
            ContactName = "Alice",
            ContactEmailAddress = "alice@acme.org"
        };

        var vendor = command.Execute(request);

        vendor.VendorId.ShouldBeGreaterThan(0);

        var persisted = usersContext.Vendors
            .Include(v => v.VendorNamespacePrefixes)
            .Include(v => v.Users)
            .Single(v => v.VendorId == vendor.VendorId);

        persisted.VendorNamespacePrefixes.ShouldNotBeNull();
        persisted.VendorNamespacePrefixes.Count.ShouldBe(2);
        persisted.VendorNamespacePrefixes.Select(p => p.NamespacePrefix).ShouldContain("https://a.org/ns");
        persisted.VendorNamespacePrefixes.Select(p => p.NamespacePrefix).ShouldContain("https://b.org/ns");
    }

    [Test]
    public void Execute_WithPaddedFields_TrimsCompanyContactNameAndEmail()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddVendorCommand_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var command = new AddVendorCommand(usersContext);
        var request = new AddVendorModelStub
        {
            Company = "  Acme Vendor  ",
            NamespacePrefixes = "https://acme.org/ns",
            ContactName = "  Alice  ",
            ContactEmailAddress = "  alice@acme.org  "
        };

        var vendor = command.Execute(request);

        vendor.VendorId.ShouldBeGreaterThan(0);

        var persisted = usersContext.Vendors
            .Include(v => v.VendorNamespacePrefixes)
            .Include(v => v.Users)
            .Single(v => v.VendorId == vendor.VendorId);

        persisted.VendorName.ShouldBe("Acme Vendor");
        persisted.Users.Single().FullName.ShouldBe("Alice");
        persisted.Users.Single().Email.ShouldBe("alice@acme.org");
    }

    [Test]
    public void Execute_WithNullCompanyContactNameAndEmail_PersistsVendorWithNullFields()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddVendorCommand_{Guid.NewGuid()}")
            .Options;
        using var usersContext = new SqlServerUsersContext(contextOptions);

        var command = new AddVendorCommand(usersContext);
        var request = new AddVendorModelStub
        {
            Company = null,
            NamespacePrefixes = "https://acme.org/ns",
            ContactName = null,
            ContactEmailAddress = null
        };

        var vendor = command.Execute(request);

        vendor.VendorId.ShouldBeGreaterThan(0);

        var persisted = usersContext.Vendors
            .Include(v => v.VendorNamespacePrefixes)
            .Include(v => v.Users)
            .Single(v => v.VendorId == vendor.VendorId);

        persisted.VendorName.ShouldBeNull();
        persisted.Users.Single().FullName.ShouldBeNull();
        persisted.Users.Single().Email.ShouldBeNull();
    }

    private sealed class AddVendorModelStub : IAddVendorModel
    {
        public string? Company { get; set; }
        public string? NamespacePrefixes { get; set; }
        public string? ContactName { get; set; }
        public string? ContactEmailAddress { get; set; }
    }
}

#nullable restore


