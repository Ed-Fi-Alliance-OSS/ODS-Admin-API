// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.DBTests.Database.QueryTests;

[TestFixture]
public class GetOdsInstanceDerivativeByIdQueryTests : PlatformUsersContextTestBase
{
    [Test]
    public void Should_Retreive_OdsInstanceDerivative()
    {
        var odsInstance = new OdsInstance
        {
            Name = "ODS Instance Name",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        var odsInstanceDerivative1 = new OdsInstanceDerivative
        {
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False",
            DerivativeType = "ReadReplica",
            OdsInstance = odsInstance
        };

        var odsInstanceDerivative2 = new OdsInstanceDerivative
        {
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods2;Integrated Security=True;Encrypt=False",
            DerivativeType = "Snapshot",
            OdsInstance = odsInstance
        };

        Save(odsInstance, odsInstanceDerivative1, odsInstanceDerivative2);

        OdsInstanceDerivative result = null;
        Transaction(usersContext =>
        {
            var query = new GetOdsInstanceDerivativeByIdQuery(usersContext);
            result = query.Execute(odsInstanceDerivative1.OdsInstanceDerivativeId);
            result.ShouldNotBeNull();
            result.OdsInstanceDerivativeId.ShouldBe(odsInstanceDerivative1.OdsInstanceDerivativeId);
            result.OdsInstance.OdsInstanceId.ShouldBe(odsInstanceDerivative1.OdsInstance.OdsInstanceId);
            result.DerivativeType.ShouldBe(odsInstanceDerivative1.DerivativeType);
            result.ConnectionString.ShouldBe(odsInstanceDerivative1.ConnectionString);
        });
    }

}



