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
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Vendors
{
    [TestFixture]
    public class DeleteVendorTests
    {
        [Test]
        public async Task Handle_WithExistingVendor_DeletesVendorAndReturnsOk()
        {
            var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
                .UseInMemoryDatabase(databaseName: $"DeleteVendor_{Guid.NewGuid()}")
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
            var deleteVendorCommand = new DeleteVendorCommand(usersContext, deleteApplicationCommand);

            var result = await DeleteVendor.Handle(deleteVendorCommand, vendor.VendorId);

            result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<object>>();
            usersContext.Vendors.Any(v => v.VendorId == vendor.VendorId).ShouldBeFalse();
            usersContext.Users.Any().ShouldBeFalse();
        }

        [Test]
        public void Handle_WithUnknownVendorId_ThrowsNotFoundException()
        {
            var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
                .UseInMemoryDatabase(databaseName: $"DeleteVendor_{Guid.NewGuid()}")
                .Options;
            using var usersContext = new SqlServerUsersContext(contextOptions);

            var deleteApplicationCommand = A.Fake<IDeleteApplicationCommand>();
            var deleteVendorCommand = new DeleteVendorCommand(usersContext, deleteApplicationCommand);

            Should.Throw<NotFoundException<int>>(() => DeleteVendor.Handle(deleteVendorCommand, 999).GetAwaiter().GetResult());
        }
    }
}
