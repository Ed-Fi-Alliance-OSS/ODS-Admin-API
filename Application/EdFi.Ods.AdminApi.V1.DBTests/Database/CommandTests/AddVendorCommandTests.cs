// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V1.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V1.DBTests.Database.CommandTests;

[TestFixture]
public class AddVendorCommandTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldAddVendor()
    {
        var newVendor = new Mock<IAddVendorModel>();
        newVendor.Setup(x => x.Company).Returns("test vendor");
        newVendor.Setup(x => x.NamespacePrefixes).Returns("http://www.test.com/");
        newVendor.Setup(x => x.ContactName).Returns("test user");
        newVendor.Setup(x => x.ContactEmailAddress).Returns("test@test.com");

        var id = 0;
        Transaction(usersContext =>
        {
            var command = new AddVendorCommand(usersContext);

            id = command.Execute(newVendor.Object).VendorId;
            id.ShouldBeGreaterThan(0);
        });

        Transaction(usersContext =>
        {
            var vendor = usersContext.Vendors
            .Include(x => x.VendorNamespacePrefixes)
            .Include(x => x.Users)
            .Single(v => v.VendorId == id);
            vendor.VendorName.ShouldBe("test vendor");
            vendor.VendorNamespacePrefixes.First().NamespacePrefix.ShouldBe("http://www.test.com/");
            vendor.Users.Single().FullName.ShouldBe("test user");
            vendor.Users.Single().Email.ShouldBe("test@test.com");
        });
    }

    [Test]
    public void ShouldAddVendorIfMultipleNamespacePrefixes()
    {
        var newVendor = new Mock<IAddVendorModel>();
        var namespacePrefixes = new List<string>
        {
            "http://www.test1.com/",
            "http://www.test2.com/",
            "http://www.test3.com/"
        };
        newVendor.Setup(x => x.Company).Returns("test vendor");
        newVendor.Setup(x => x.NamespacePrefixes).Returns(namespacePrefixes.ToDelimiterSeparated());
        newVendor.Setup(x => x.ContactName).Returns("test user");
        newVendor.Setup(x => x.ContactEmailAddress).Returns("test@test.com");

        var id = 0;
        Transaction(usersContext =>
        {
            var command = new AddVendorCommand(usersContext);

            id = command.Execute(newVendor.Object).VendorId;
            id.ShouldBeGreaterThan(0);
        });

        Transaction(usersContext =>
        {
            var vendor = usersContext.Vendors
           .Include(x => x.VendorNamespacePrefixes)
           .Include(x => x.Users).Single(v => v.VendorId == id);
            vendor.VendorName.ShouldBe("test vendor");
            vendor.VendorNamespacePrefixes.Select(x => x.NamespacePrefix).ShouldBe(namespacePrefixes);
            vendor.Users.Single().FullName.ShouldBe("test user");
            vendor.Users.Single().Email.ShouldBe("test@test.com");
        });
    }

    [TestCase("http://www.test1.com/, http://www.test2.com/,", "http://www.test1.com/,http://www.test2.com/")]
    [TestCase(", ,", "")]
    [TestCase(" ", "")]
    [TestCase(null, "")]
    public void ShouldNotAddEmptyNamespacePrefixesWhileAddingVendor(string inputNamespacePrefixes, string expectedNamespacePrefixes)
    {
        var newVendor = new Mock<IAddVendorModel>();
        newVendor.Setup(x => x.Company).Returns("test vendor");
        newVendor.Setup(x => x.NamespacePrefixes).Returns(inputNamespacePrefixes);
        newVendor.Setup(x => x.ContactName).Returns("test user");
        newVendor.Setup(x => x.ContactEmailAddress).Returns("test@test.com");

        var id = 0;
        Transaction(usersContext =>
        {
            var command = new AddVendorCommand(usersContext);

            id = command.Execute(newVendor.Object).VendorId;
            id.ShouldBeGreaterThan(0);
        });

        Transaction(usersContext =>
        {
            var vendor = usersContext.Vendors.
            Include(x => x.VendorNamespacePrefixes)
           .Include(x => x.Users)
           .Single(v => v.VendorId == id);
            vendor.VendorName.ShouldBe("test vendor");
            vendor.VendorNamespacePrefixes.Select(x => x.NamespacePrefix).ToDelimiterSeparated().ShouldBe(expectedNamespacePrefixes);
            vendor.Users.Single().FullName.ShouldBe("test user");
            vendor.Users.Single().Email.ShouldBe("test@test.com");
        });
    }
}
