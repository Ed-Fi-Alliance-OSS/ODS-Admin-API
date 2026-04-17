// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using NUnit.Framework;
using Shouldly;
using System.Linq;

namespace EdFi.Ods.AdminApi.DBTests.V3.Database.CommandTests;

[TestFixture]
public class DeleteOdsInstanceDerivativeTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldDeleteOdsInstanceDerivative()
    {
        var odsInstance = new OdsInstance
        {
            Name = "ODS Instance Name",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        var newOdsInstanceDerivative = new OdsInstanceDerivative()
        {
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False",
            DerivativeType = "ReadReplica",
            OdsInstance = odsInstance
        };
        Save(newOdsInstanceDerivative);
        var odsInstanceDerivativeId = newOdsInstanceDerivative.OdsInstanceDerivativeId;

        Transaction(usersContext =>
        {
            var deleteOdsInstanceDerivativeCommand = new DeleteOdsInstanceDerivativeCommand(usersContext);
            deleteOdsInstanceDerivativeCommand.Execute(odsInstanceDerivativeId);
        });

        Transaction(usersContext => usersContext.OdsInstanceDerivatives.Where(v => v.OdsInstanceDerivativeId == odsInstanceDerivativeId).ToArray()).ShouldBeEmpty();
    }
}
