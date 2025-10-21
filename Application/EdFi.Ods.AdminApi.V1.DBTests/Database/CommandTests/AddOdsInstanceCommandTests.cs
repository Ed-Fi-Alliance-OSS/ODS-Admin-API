// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Commands;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V1.DBTests.Database.CommandTests;

public class AddOdsInstanceCommandTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldAddOdsInstance()
    {
        var newOdsInstance = new Mock<IAddOdsInstanceModel>();
        newOdsInstance.Setup(x => x.Name).Returns("test ods instance");
        newOdsInstance.Setup(x => x.InstanceType).Returns("test type");
        newOdsInstance.Setup(x => x.Status).Returns("test status");
        newOdsInstance.Setup(x => x.IsExtended).Returns(true);
        newOdsInstance.Setup(x => x.Version).Returns("test version");

        var id = 0;
        Transaction(usersContext =>
        {
            var command = new AddOdsInstanceCommand(usersContext);

            id = command.Execute(newOdsInstance.Object).OdsInstanceId;
            id.ShouldBeGreaterThan(0);
        });

        Transaction(usersContext =>
        {
            var odsInstance = usersContext.OdsInstances
            .Single(v => v.OdsInstanceId == id);
            odsInstance.Name.ShouldBe("test ods instance");
            odsInstance.InstanceType.ShouldBe("test type");
            odsInstance.Status.ShouldBe("test status");
            odsInstance.IsExtended.ShouldBe(true);
            odsInstance.Version.ShouldBe("test version");
        });
    }
}
