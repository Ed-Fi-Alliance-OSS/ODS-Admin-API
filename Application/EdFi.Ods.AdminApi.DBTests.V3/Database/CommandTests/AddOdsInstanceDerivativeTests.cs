// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.


using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Moq;
using NUnit.Framework;
using Shouldly;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.DBTests.V3.Database.CommandTests;

[TestFixture]
public class AddOdsInstanceDerivativeTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldAddOdsInstanceDerivative()
    {
        var odsInstance = new OdsInstance
        {
            Name = "ODS Instance Name",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        Save(odsInstance);

        var derivativeType = "ReadReplica";
        var connectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False";

        var newOdsInstanceDerivative = new Mock<IAddOdsInstanceDerivativeModel>();
        newOdsInstanceDerivative.Setup(x => x.OdsInstanceId).Returns(odsInstance.OdsInstanceId);
        newOdsInstanceDerivative.Setup(x => x.DerivativeType).Returns(derivativeType);
        newOdsInstanceDerivative.Setup(x => x.ConnectionString).Returns(connectionString);

        var id = 0;
        Transaction(usersContext =>
        {
            var command = new AddOdsInstanceDerivativeCommand(usersContext);
            id = command.Execute(newOdsInstanceDerivative.Object).OdsInstanceDerivativeId;
            id.ShouldBeGreaterThan(0);
        });

        Transaction(usersContext =>
        {
            var odsInstanceDerivative = usersContext.OdsInstanceDerivatives
            .Include(o => o.OdsInstance)
            .Single(v => v.OdsInstanceDerivativeId == id);
            odsInstanceDerivative.OdsInstance.OdsInstanceId.ShouldBe(odsInstance.OdsInstanceId);
            odsInstanceDerivative.DerivativeType.ShouldBe(derivativeType);
            odsInstanceDerivative.ConnectionString.ShouldBe(connectionString);
        });
    }

    [Test]
    public void ShouldFailOdsInstanceDerivativeCombinedKey()
    {
        var odsInstance = new OdsInstance
        {
            Name = "ODS Instance Name",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        Save(odsInstance);

        var derivativeType = "ReadReplica";
        var connectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False";
        var newOdsInstanceDerivative = new Mock<IAddOdsInstanceDerivativeModel>();
        newOdsInstanceDerivative.Setup(x => x.OdsInstanceId).Returns(odsInstance.OdsInstanceId);
        newOdsInstanceDerivative.Setup(x => x.DerivativeType).Returns(derivativeType);
        newOdsInstanceDerivative.Setup(x => x.ConnectionString).Returns(connectionString);
        var id = 0;
        Transaction(usersContext =>
        {
            var command = new AddOdsInstanceDerivativeCommand(usersContext);
            id = command.Execute(newOdsInstanceDerivative.Object).OdsInstanceDerivativeId;
            id.ShouldBeGreaterThan(0);
        });

        var newDerivativeType = "ReadReplica";
        var newOdsInstanceDerivative2 = new Mock<IAddOdsInstanceDerivativeModel>();
        newOdsInstanceDerivative2.Setup(x => x.OdsInstanceId).Returns(odsInstance.OdsInstanceId);
        newOdsInstanceDerivative2.Setup(x => x.DerivativeType).Returns(newDerivativeType);
        newOdsInstanceDerivative2.Setup(x => x.ConnectionString).Returns(connectionString);
        var newId = 0;
        Assert.Throws<DbUpdateException>(() =>
        {
            Transaction(usersContext =>
            {
                var command = new AddOdsInstanceDerivativeCommand(usersContext);
                newId = command.Execute(newOdsInstanceDerivative2.Object).OdsInstanceDerivativeId;
                newId.ShouldBeGreaterThan(0);
            });
        });
        
    }

    [Test]
    [Ignore("Column is allowing null values")]
    public void ShouldFailToAddWhenConnectionStringIsEmpty()
    {
        var odsInstance = new OdsInstance
        {
            Name = "ODS Instance Name",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        Save(odsInstance);

        var derivativeType = "ReadReplica";

        var newOdsInstanceDerivative = new Mock<IAddOdsInstanceDerivativeModel>();
        newOdsInstanceDerivative.Setup(x => x.OdsInstanceId).Returns(odsInstance.OdsInstanceId);
        newOdsInstanceDerivative.Setup(x => x.DerivativeType).Returns(derivativeType);

        var id = 0;
        Assert.Throws<DbUpdateException>(() =>
        {
            Transaction(usersContext =>
            {
                var command = new AddOdsInstanceDerivativeCommand(usersContext);
                id = command.Execute(newOdsInstanceDerivative.Object).OdsInstanceDerivativeId;
            });
        });
        Assert.That(id, Is.EqualTo(0));
    }

    [Test]
    public void ShouldFailToAddWhenDerivativeTypeIsEmpty()
    {
        var odsInstance = new OdsInstance
        {
            Name = "ODS Instance Name",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        Save(odsInstance);

        var connectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False";

        var newOdsInstanceDerivative = new Mock<IAddOdsInstanceDerivativeModel>();
        newOdsInstanceDerivative.Setup(x => x.OdsInstanceId).Returns(odsInstance.OdsInstanceId);
        newOdsInstanceDerivative.Setup(x => x.ConnectionString).Returns(connectionString);

        var id = 0;
        Assert.Throws<DbUpdateException>(() =>
        {
            Transaction(usersContext =>
            {
                var command = new AddOdsInstanceDerivativeCommand(usersContext);
                id = command.Execute(newOdsInstanceDerivative.Object).OdsInstanceDerivativeId;
            });
        });
        Assert.That(id, Is.EqualTo(0));
    }

}


