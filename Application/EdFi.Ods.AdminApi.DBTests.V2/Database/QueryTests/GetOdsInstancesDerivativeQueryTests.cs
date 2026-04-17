// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V2.Infrastructure;
using EdFi.Ods.AdminApi.V2.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.V2.Database.QueryTests;

[TestFixture]
public class GetOdsInstancesDerivativeQueryTests : PlatformUsersContextTestBase
{
    [Test]
    public void Should_Retreive_OdsInstancesDerivative()
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

        Save(odsInstance, odsInstanceDerivative1);

        List<OdsInstanceDerivative> results = null;
        Transaction(usersContext =>
        {
            var query = new GetOdsInstanceDerivativesQuery(usersContext, Testing.GetAppSettings());
            results = query.Execute();
        });

        results.Any(p => p.OdsInstance.OdsInstanceId == odsInstanceDerivative1.OdsInstance.OdsInstanceId).ShouldBeTrue();
        results.Any(p => p.DerivativeType == odsInstanceDerivative1.DerivativeType).ShouldBeTrue();
        results.Any(p => p.ConnectionString == odsInstanceDerivative1.ConnectionString).ShouldBeTrue();

    }

    [Test]
    public void Should_Retreive_OdsInstancesDerivative_With_Offset_Limit()
    {
        var odsInstance = new OdsInstance
        {
            Name = "ODS Instance Name",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        var odsInstanceDerivative1 = new OdsInstanceDerivative
        {
            ConnectionString = "{ConnectionString}",
            DerivativeType = "Type1",
            OdsInstance = odsInstance
        };

        var odsInstanceDerivative2 = new OdsInstanceDerivative
        {
            ConnectionString = "{ConnectionString}",
            DerivativeType = "Type2",
            OdsInstance = odsInstance
        };

        Save(odsInstance, odsInstanceDerivative1, odsInstanceDerivative2);

        List<OdsInstanceDerivative> results = null;
        Transaction(usersContext =>
        {
            var query = new GetOdsInstanceDerivativesQuery(usersContext, Testing.GetAppSettings());
            results = query.Execute(new CommonQueryParams(1, 1));
            results.Count.ShouldBe(1);
        });

        results.Any(p => p.OdsInstance.OdsInstanceId == odsInstanceDerivative2.OdsInstance.OdsInstanceId).ShouldBeTrue();
        results.Any(p => p.DerivativeType == odsInstanceDerivative2.DerivativeType).ShouldBeTrue();
        results.Any(p => p.ConnectionString == odsInstanceDerivative2.ConnectionString).ShouldBeTrue();
    }
}
