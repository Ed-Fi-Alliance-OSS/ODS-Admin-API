// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
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
