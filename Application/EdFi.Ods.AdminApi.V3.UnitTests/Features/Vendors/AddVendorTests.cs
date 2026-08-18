// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Features.Vendors;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.Vendors
{
    [TestFixture]
    public class AddVendorTests
    {
        private static HttpContext CreateHttpContext()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost", 7214);
            return httpContext;
        }

        private static AddVendor.Validator CreateValidator()
        {
            var getVendorsQuery = A.Fake<IGetVendorsQuery>();
            A.CallTo(() => getVendorsQuery.ExistsByName(A<string>._)).Returns(false);
            return new AddVendor.Validator(getVendorsQuery);
        }

        [Test]
        public async Task Handle_WithValidRequest_ReturnsCreatedAndPersistsVendor()
        {
            var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
                .UseInMemoryDatabase(databaseName: $"AddVendor_{Guid.NewGuid()}")
                .Options;
            using var usersContext = new SqlServerUsersContext(contextOptions);

            var validator = CreateValidator();
            var command = new AddVendorCommand(usersContext);
            var request = new AddVendor.AddVendorRequest
            {
                Company = "Acme Vendor",
                NamespacePrefixes = "https://acme.org/ns",
                ContactName = "Alice",
                ContactEmailAddress = "alice@acme.org"
            };
            var httpContext = CreateHttpContext();

            var result = await AddVendor.Handle(validator, command, request, httpContext);

            result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Created>();
            (await usersContext.Vendors.AnyAsync(v => v.VendorName == "Acme Vendor")).ShouldBeTrue();
        }

        [Test]
        public void Handle_WithInvalidRequest_ThrowsValidationException()
        {
            var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
                .UseInMemoryDatabase(databaseName: $"AddVendor_{Guid.NewGuid()}")
                .Options;
            using var usersContext = new SqlServerUsersContext(contextOptions);

            var validator = CreateValidator();
            var command = new AddVendorCommand(usersContext);
            var request = new AddVendor.AddVendorRequest
            {
                Company = string.Empty,
                NamespacePrefixes = "https://acme.org/ns",
                ContactName = "Alice",
                ContactEmailAddress = "alice@acme.org"
            };
            var httpContext = CreateHttpContext();

            Should.ThrowAsync<ValidationException>(async () => await AddVendor.Handle(validator, command, request, httpContext));
        }

        [Test]
        public void Validator_WithNullNamespacePrefixes_IsValid()
        {
            var validator = CreateValidator();
            var request = new AddVendor.AddVendorRequest
            {
                Company = "Acme Vendor",
                NamespacePrefixes = null,
                ContactName = "Alice",
                ContactEmailAddress = "alice@acme.org"
            };

            var result = validator.Validate(request);

            result.IsValid.ShouldBeTrue();
        }
    }
}

