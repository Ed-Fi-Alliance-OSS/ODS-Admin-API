// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using Moq;
using NUnit.Framework;
using Shouldly;
using System.Linq;

namespace EdFi.Ods.AdminApi.DBTests.Database.CommandTests;

[TestFixture]
public class EditOdsInstanceCommandTests : PlatformUsersContextTestBase
{
    private int _odsInstanceId;

    [SetUp]
    public void Init()
    {
        var originalOdsInstance = new OdsInstance
        {
            Name = "old odsinstance name",
            InstanceType = "old odsinstance instance type",
            ConnectionString = "old odsinstance connection string",
        };


        Save(originalOdsInstance);
        _odsInstanceId = originalOdsInstance.OdsInstanceId;
    }

    [Test]
    public void ShouldEditOdsInstance()
    {
        var name = "new odsinstance name";
        var instanceType = "new odsinstance instance type";
        var connectionString = "new odsinstance connection string";
        var newOdsInstanceData = new Mock<IEditOdsInstanceModel>();
        newOdsInstanceData.Setup(v => v.Id).Returns(_odsInstanceId);
        newOdsInstanceData.Setup(v => v.Name).Returns(name);
        newOdsInstanceData.Setup(v => v.InstanceType).Returns(instanceType);
        newOdsInstanceData.Setup(v => v.ConnectionString).Returns(connectionString);

        Transaction(usersContext =>
        {
            var editOdsInstanceCommand = new EditOdsInstanceCommand(usersContext);
            editOdsInstanceCommand.Execute(newOdsInstanceData.Object);
        });

        Transaction(usersContext =>
        {
            var changedOdsInstance = usersContext.OdsInstances.Single(v => v.OdsInstanceId == _odsInstanceId);
            changedOdsInstance.Name.ShouldBe(name);
            changedOdsInstance.InstanceType.ShouldBe(instanceType);
            changedOdsInstance.ConnectionString.ShouldBe(connectionString);
        });
    }

    [Test]
    public void ShouldEditOdsInstanceWithEmptyInstanceType()
    {
        var name = "new odsinstance name";
        var connectionString = "new odsinstance connection string";
        var newOdsInstanceData = new Mock<IEditOdsInstanceModel>();
        newOdsInstanceData.Setup(v => v.Id).Returns(_odsInstanceId);
        newOdsInstanceData.Setup(v => v.Name).Returns(name);
        newOdsInstanceData.Setup(v => v.ConnectionString).Returns(connectionString);

        Transaction(usersContext =>
        {
            var editOdsInstanceCommand = new EditOdsInstanceCommand(usersContext);
            editOdsInstanceCommand.Execute(newOdsInstanceData.Object);
        });

        Transaction(usersContext =>
        {
            var changedOdsInstance = usersContext.OdsInstances.Single(v => v.OdsInstanceId == _odsInstanceId);
            changedOdsInstance.Name.ShouldBe(name);
            changedOdsInstance.InstanceType.ShouldBeNullOrEmpty();
            changedOdsInstance.ConnectionString.ShouldBe(connectionString);
        });
    }
}
