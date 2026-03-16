// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

#nullable enable

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Commands;

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

    private sealed class AddVendorModelStub : IAddVendorModel
    {
        public string? Company { get; set; }
        public string? NamespacePrefixes { get; set; }
        public string? ContactName { get; set; }
        public string? ContactEmailAddress { get; set; }
    }
}

#nullable restore
