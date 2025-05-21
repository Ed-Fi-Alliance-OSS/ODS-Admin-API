// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.CommandTests;

[TestFixture]
public class AddOdsInstanceCommandTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldAddOdsInstance()
    {
        var odsInstanceName = $"Test-OdsInstance{Guid.NewGuid()}";
        var odsInstanceType = $"Test-OdsInstanceType-{Guid.NewGuid()}";
        var odsInstanceConnectionString = "ConnectionString";
        var newOdsInstance = new Mock<IAddOdsInstanceModel>();
        newOdsInstance.Setup(x => x.Name).Returns(odsInstanceName);
        newOdsInstance.Setup(x => x.InstanceType).Returns(odsInstanceType);
        newOdsInstance.Setup(x => x.ConnectionString).Returns(odsInstanceConnectionString);


        var id = 0;
        Transaction(usersContext =>
        {
            var command = new AddOdsInstanceCommand(usersContext);
            id = command.Execute(newOdsInstance.Object).OdsInstanceId;
            id.ShouldBeGreaterThan(0);
        });

        Transaction(usersContext =>
        {
            var profile = usersContext.OdsInstances.Single(v => v.OdsInstanceId == id);
            profile.Name.ShouldBe(odsInstanceName);
            profile.InstanceType.ShouldBe(odsInstanceType);
            profile.ConnectionString.ShouldBe(odsInstanceConnectionString);
        });
    }

    [Test]
    public void ShouldAddOdsInstanceWithEmptyInstanceType()
    {
        var odsInstanceName = $"Test-OdsInstance{Guid.NewGuid()}";
        var odsInstanceConnectionString = "ConnectionString";
        var newOdsInstance = new Mock<IAddOdsInstanceModel>();
        newOdsInstance.Setup(x => x.Name).Returns(odsInstanceName);
        newOdsInstance.Setup(x => x.ConnectionString).Returns(odsInstanceConnectionString);


        var id = 0;
        Transaction(usersContext =>
        {
            var command = new AddOdsInstanceCommand(usersContext);
            id = command.Execute(newOdsInstance.Object).OdsInstanceId;
            id.ShouldBeGreaterThan(0);
        });

        Transaction(usersContext =>
        {
            var profile = usersContext.OdsInstances.Single(v => v.OdsInstanceId == id);
            profile.Name.ShouldBe(odsInstanceName);
            profile.InstanceType.ShouldBeNullOrEmpty();
            profile.ConnectionString.ShouldBe(odsInstanceConnectionString);
        });
    }
}
