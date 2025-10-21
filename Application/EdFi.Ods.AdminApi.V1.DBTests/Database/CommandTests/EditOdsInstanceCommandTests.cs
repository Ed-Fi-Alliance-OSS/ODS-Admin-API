// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Moq;
using NUnit.Framework;
using Shouldly;
using System.Linq;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V1.DBTests.Database.CommandTests;

[TestFixture]
internal class EditOdsInstanceCommandTests : PlatformUsersContextTestBase
{
    private int _odsInstanceId;

    [SetUp]
    public void Init()
    {
        var originalOdsInstance = new OdsInstance
        {
            Name = "old ods instance name",
            InstanceType = "old type",
            Status = "old status",
            IsExtended = false,
            Version = "old version",
        };

        Save(originalOdsInstance);
        _odsInstanceId = originalOdsInstance.OdsInstanceId;
    }

    [Test]
    public void ShouldEditVendor()
    {
        var newOdsInstanceData = new Mock<IEditOdsInstanceModel>();
        newOdsInstanceData.Setup(v => v.OdsInstanceId).Returns(_odsInstanceId);
        newOdsInstanceData.Setup(v => v.Name).Returns("new ods instance name");
        newOdsInstanceData.Setup(v => v.InstanceType).Returns("new type");
        newOdsInstanceData.Setup(v => v.Status).Returns("new status");
        newOdsInstanceData.Setup(v => v.IsExtended).Returns(true);
        newOdsInstanceData.Setup(v => v.Version).Returns("new version");

        Transaction(usersContext =>
        {
            var editOdsInstanceCommand = new EditOdsInstanceCommand(usersContext);
            editOdsInstanceCommand.Execute(newOdsInstanceData.Object);
        });

        Transaction(usersContext =>
        {
            var changedOdsInstance = usersContext.OdsInstances
            .Single(v => v.OdsInstanceId == _odsInstanceId);
            changedOdsInstance.Name.ShouldBe("new ods instance name");
            changedOdsInstance.InstanceType.ShouldBe("new type");
            changedOdsInstance.Status.ShouldBe("new status");
            changedOdsInstance.IsExtended.ShouldBe(true);
            changedOdsInstance.Version.ShouldBe("new version");
        });
    }
}
