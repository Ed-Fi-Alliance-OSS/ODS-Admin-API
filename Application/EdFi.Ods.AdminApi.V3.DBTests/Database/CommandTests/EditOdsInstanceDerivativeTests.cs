// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Moq;
using NUnit.Framework;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.V3.DBTests.Database.CommandTests;

[TestFixture]
public class EditOdsInstanceDerivativeTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldEditOdsInstanceDerivative()
    {
        var odsInstance1 = new OdsInstance
        {
            Name = "ODS Instance Name",
            InstanceType = "Ods1",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        var odsInstance2 = new OdsInstance
        {
            Name = "ODS Instance Name",
            InstanceType = "Ods2",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };
        Save(odsInstance2);

        var derivativeType = "ReadReplica";
        var connectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False";
        var newOdsInstanceDerivative = new OdsInstanceDerivative
        {
            ConnectionString = connectionString,
            DerivativeType = derivativeType,
            OdsInstance = odsInstance1
        };
        Save(newOdsInstanceDerivative);

        var updateDerivativeType = "ReadReplica";
        var updateConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods_2;Integrated Security=True;Encrypt=False";
        var editOdsInstanceDerivative = new Mock<IEditOdsInstanceDerivativeModel>();
        editOdsInstanceDerivative.Setup(x => x.OdsInstanceId).Returns(odsInstance2.OdsInstanceId);
        editOdsInstanceDerivative.Setup(x => x.DerivativeType).Returns(updateDerivativeType);
        editOdsInstanceDerivative.Setup(x => x.ConnectionString).Returns(updateConnectionString);
        editOdsInstanceDerivative.Setup(x => x.Id).Returns(newOdsInstanceDerivative.OdsInstanceDerivativeId);

        Transaction(usersContext =>
        {
            var command = new EditOdsInstanceDerivativeCommand(usersContext);
            var updatedOdsInstanceDerivative = command.Execute(editOdsInstanceDerivative.Object);
            updatedOdsInstanceDerivative.ShouldNotBeNull();
            updatedOdsInstanceDerivative.OdsInstanceDerivativeId.ShouldBeGreaterThan(0);
            updatedOdsInstanceDerivative.OdsInstanceDerivativeId.ShouldBe(newOdsInstanceDerivative.OdsInstanceDerivativeId);
            updatedOdsInstanceDerivative.OdsInstance.OdsInstanceId.ShouldBe(odsInstance2.OdsInstanceId);
            updatedOdsInstanceDerivative.DerivativeType.ShouldBe(updateDerivativeType);
            updatedOdsInstanceDerivative.ConnectionString.ShouldBe(updateConnectionString);
        });
    }

    [Test]
    public void ShouldFailOdsInstanceDerivativeCombinedKey()
    {
        var odsInstance1 = new OdsInstance
        {
            Name = "ODS Instance Name",
            InstanceType = "Ods1",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        var derivativeType = "ReadReplica";
        var connectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False";
        var newOdsInstanceDerivative = new OdsInstanceDerivative
        {
            ConnectionString = connectionString,
            DerivativeType = derivativeType,
            OdsInstance = odsInstance1
        };

        var newDerivativeType = "Snapshot";
        var newOdsInstanceDerivative2 = new OdsInstanceDerivative
        {
            ConnectionString = connectionString,
            DerivativeType = newDerivativeType,
            OdsInstance = odsInstance1
        };
        Save(newOdsInstanceDerivative, newOdsInstanceDerivative2);

        var updateDerivativeType = "Snapshot";
        var updateConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods_2;Integrated Security=True;Encrypt=False";
        var editOdsInstanceDerivative = new Mock<IEditOdsInstanceDerivativeModel>();
        editOdsInstanceDerivative.Setup(x => x.OdsInstanceId).Returns(odsInstance1.OdsInstanceId);
        editOdsInstanceDerivative.Setup(x => x.DerivativeType).Returns(updateDerivativeType);
        editOdsInstanceDerivative.Setup(x => x.ConnectionString).Returns(updateConnectionString);
        editOdsInstanceDerivative.Setup(x => x.Id).Returns(newOdsInstanceDerivative.OdsInstanceDerivativeId);

        Assert.Throws<DbUpdateException>(() =>
        {
            Transaction(usersContext =>
            {
                var command = new EditOdsInstanceDerivativeCommand(usersContext);
                var updatedOdsInstanceDerivative = command.Execute(editOdsInstanceDerivative.Object);
                updatedOdsInstanceDerivative.ShouldNotBeNull();
                updatedOdsInstanceDerivative.OdsInstanceDerivativeId.ShouldBeGreaterThan(0);
                updatedOdsInstanceDerivative.OdsInstanceDerivativeId.ShouldBe(newOdsInstanceDerivative.OdsInstanceDerivativeId);
                updatedOdsInstanceDerivative.OdsInstance.OdsInstanceId.ShouldBe(odsInstance1.OdsInstanceId);
                updatedOdsInstanceDerivative.DerivativeType.ShouldBe(updateDerivativeType);
                updatedOdsInstanceDerivative.ConnectionString.ShouldBe(updateConnectionString);
            });
        });
    }

    [Test]
    public void ShouldFailToEditWithInvalidId()
    {
        var odsInstance = new OdsInstance
        {
            Name = "ODS Instance Name",
            InstanceType = "Ods1",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };
        Save(odsInstance);

        var updateDerivativeType = "ReadReplica";
        var updateConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods_2;Integrated Security=True;Encrypt=False";
        var editOdsInstanceDerivative = new Mock<IEditOdsInstanceDerivativeModel>();
        editOdsInstanceDerivative.Setup(x => x.OdsInstanceId).Returns(odsInstance.OdsInstanceId);
        editOdsInstanceDerivative.Setup(x => x.DerivativeType).Returns(updateDerivativeType);
        editOdsInstanceDerivative.Setup(x => x.ConnectionString).Returns(updateConnectionString);
        editOdsInstanceDerivative.Setup(x => x.Id).Returns(-1);

        Assert.Throws<NotFoundException<int>>(() =>
        {
            Transaction(usersContext =>
            {
                var command = new EditOdsInstanceDerivativeCommand(usersContext);
                var updatedOdsInstanceDerivative = command.Execute(editOdsInstanceDerivative.Object);
            });
        });
    }

}



