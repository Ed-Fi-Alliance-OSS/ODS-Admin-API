// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Commands;
using NUnit.Framework;
using Shouldly;
using VendorUser = EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models.User;

namespace EdFi.Ods.AdminApi.V1.DBTests.Database.CommandTests;

[TestFixture]
public class DeleteVendorCommandTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldDeleteVendor()
    {
        var newVendor = new Vendor();
        Save(newVendor);
        var vendorId = newVendor.VendorId;

        Transaction(usersContext =>
        {
            var deleteVendorCommand = new DeleteVendorCommand(usersContext, null);
            deleteVendorCommand.Execute(vendorId);
        });

        Transaction(usersContext => usersContext.Vendors.Where(v => v.VendorId == vendorId).ToArray()).ShouldBeEmpty();
    }

    [Test]
    public void ShouldDeleteVendorWithApplication()
    {
        var newVendor = new Vendor { VendorName = "test vendor" };
        var newApplication = new Application { ApplicationName = "test application", OperationalContextUri = OperationalContext.DefaultOperationalContextUri };
        newVendor.Applications.Add(newApplication);
        Save(newVendor);
        var vendorId = newVendor.VendorId;
        var applicationId = newApplication.ApplicationId;
        applicationId.ShouldBeGreaterThan(0);

        Transaction(usersContext =>
        {
            var deleteApplicationCommand = new DeleteApplicationCommand(usersContext);
            var deleteVendorCommand = new DeleteVendorCommand(usersContext, deleteApplicationCommand);
            deleteVendorCommand.Execute(vendorId);
        });

        Transaction(usersContext => usersContext.Vendors.Where(v => v.VendorId == vendorId).ToArray()).ShouldBeEmpty();
        Transaction(usersContext => usersContext.Applications.Where(a => a.ApplicationId == applicationId).ToArray()).ShouldBeEmpty();
    }

    [Test]
    public void ShouldDeleteVendorWithUser()
    {
        var newVendor = new Vendor { VendorName = "test vendor" };
        var newUser = new VendorUser
        {
            FullName = "test user",
            Email = "testuser@example.com", // Required property 'Email' set  
            Vendor = newVendor // Required property 'Vendor' set  
        };
        newVendor.Users.Add(newUser);
        Save(newVendor);
        var vendorId = newVendor.VendorId;
        var userId = newUser.UserId;
        userId.ShouldBeGreaterThan(0);

        Transaction(usersContext =>
        {
            var deleteVendorCommand = new DeleteVendorCommand(usersContext, null);
            deleteVendorCommand.Execute(vendorId);
        });

        Transaction(usersContext => usersContext.Vendors.Where(v => v.VendorId == vendorId).ToArray()).ShouldBeEmpty();
        Transaction(usersContext => usersContext.Users.Where(u => u.UserId == userId).ToArray()).ShouldBeEmpty();
    }
}
