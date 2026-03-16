// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Features.Vendors;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using FakeItEasy;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Vendors
{
    [TestFixture]
    public class AddVendorTests
    {
        [Test]
        public async Task Handle_WithValidRequest_ReturnsCreatedAndPersistsVendor()
        {
            var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
                .UseInMemoryDatabase(databaseName: $"AddVendor_{Guid.NewGuid()}")
                .Options;
            using var usersContext = new SqlServerUsersContext(contextOptions);

            var validator = new AddVendor.Validator();
            var command = new AddVendorCommand(usersContext);
            var mapper = A.Fake<IMapper>();
            var request = new AddVendor.AddVendorRequest
            {
                Company = "Acme Vendor",
                NamespacePrefixes = "https://acme.org/ns",
                ContactName = "Alice",
                ContactEmailAddress = "alice@acme.org"
            };

            var result = await AddVendor.Handle(validator, command, mapper, request);

            result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Created>();
            usersContext.Vendors.Any(v => v.VendorName == "Acme Vendor").ShouldBeTrue();
        }

        [Test]
        public void Handle_WithInvalidRequest_ThrowsValidationException()
        {
            var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
                .UseInMemoryDatabase(databaseName: $"AddVendor_{Guid.NewGuid()}")
                .Options;
            using var usersContext = new SqlServerUsersContext(contextOptions);

            var validator = new AddVendor.Validator();
            var command = new AddVendorCommand(usersContext);
            var mapper = A.Fake<IMapper>();
            var request = new AddVendor.AddVendorRequest
            {
                Company = string.Empty,
                NamespacePrefixes = "https://acme.org/ns",
                ContactName = "Alice",
                ContactEmailAddress = "alice@acme.org"
            };

            Should.ThrowAsync<ValidationException>(async () => await AddVendor.Handle(validator, command, mapper, request));
        }
    }
}
