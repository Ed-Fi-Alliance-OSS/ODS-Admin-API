// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Features.Vendors;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Vendors
{
    [TestFixture]
    public class EditVendorTests
    {
        [Test]
        public async Task Handle_WithValidRequest_ReturnsOkAndUpdatesVendor()
        {
            var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
                .UseInMemoryDatabase(databaseName: $"EditVendor_{Guid.NewGuid()}")
                .Options;
            using var usersContext = new SqlServerUsersContext(contextOptions);

            var vendor = new Vendor
            {
                VendorName = "Original Vendor",
                VendorNamespacePrefixes =
                [
                    new VendorNamespacePrefix { NamespacePrefix = "https://old.org/ns" }
                ],
                Users =
                [
                    new User { FullName = "Original Contact", Email = "original@acme.org" }
                ]
            };
            usersContext.Vendors.Add(vendor);
            await usersContext.SaveChangesAsync();

            var command = new EditVendorCommand(usersContext);
            var validator = new EditVendor.Validator();
            var request = new EditVendor.EditVendorRequest
            {
                Company = "Updated Vendor",
                NamespacePrefixes = "https://new.org/ns",
                ContactName = "Updated Contact",
                ContactEmailAddress = "updated@acme.org"
            };

            var result = await EditVendor.Handle(command, validator, request, vendor.VendorId);

            result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok>();

            var updatedVendor = usersContext.Vendors
                .Include(v => v.VendorNamespacePrefixes)
                .Include(v => v.Users)
                .SingleAsync(v => v.VendorId == vendor.VendorId);

            var updatedVendorResult = await updatedVendor;

            updatedVendorResult.VendorName.ShouldBe("Updated Vendor");
            updatedVendorResult.VendorNamespacePrefixes.Single().NamespacePrefix.ShouldBe("https://new.org/ns");
            updatedVendorResult.Users.Single().FullName.ShouldBe("Updated Contact");
            updatedVendorResult.Users.Single().Email.ShouldBe("updated@acme.org");
        }

        [Test]
        public void Handle_WithInvalidId_ThrowsValidationException()
        {
            var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
                .UseInMemoryDatabase(databaseName: $"EditVendor_{Guid.NewGuid()}")
                .Options;
            using var usersContext = new SqlServerUsersContext(contextOptions);

            var command = new EditVendorCommand(usersContext);
            var validator = new EditVendor.Validator();
            var request = new EditVendor.EditVendorRequest
            {
                Company = "Updated Vendor",
                NamespacePrefixes = "https://new.org/ns",
                ContactName = "Updated Contact",
                ContactEmailAddress = "updated@acme.org"
            };

            Should.ThrowAsync<ValidationException>(async () => await EditVendor.Handle(command, validator, request, 0));
        }

        [Test]
        public void Handle_WithUnknownVendorId_ThrowsNotFoundException()
        {
            var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
                .UseInMemoryDatabase(databaseName: $"EditVendor_{Guid.NewGuid()}")
                .Options;
            using var usersContext = new SqlServerUsersContext(contextOptions);

            var command = new EditVendorCommand(usersContext);
            var validator = new EditVendor.Validator();
            var request = new EditVendor.EditVendorRequest
            {
                Company = "Updated Vendor",
                NamespacePrefixes = "https://new.org/ns",
                ContactName = "Updated Contact",
                ContactEmailAddress = "updated@acme.org"
            };

            Should.Throw<NotFoundException<int>>(() => EditVendor.Handle(command, validator, request, 999).GetAwaiter().GetResult());
        }
    }
}
