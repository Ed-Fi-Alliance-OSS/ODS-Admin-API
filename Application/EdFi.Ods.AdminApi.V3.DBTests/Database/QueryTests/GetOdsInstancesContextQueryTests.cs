// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.DBTests.Database.QueryTests;

[TestFixture]
public class GetOdsInstancesContextQueryTests : PlatformUsersContextTestBase
{
    [Test]
    public void Should_Retreive_OdsInstancesContext()
    {
        var odsInstance = new OdsInstance
        {
            Name = "ODS Instance Name",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        var odsInstanceContext1 = new OdsInstanceContext
        {
            ContextKey = "contextKey",
            ContextValue = "contextValue",
            OdsInstance = odsInstance
        };

        Save(odsInstance, odsInstanceContext1);

        List<OdsInstanceContext> results = null;
        Transaction(usersContext =>
        {
            var query = new GetOdsInstanceContextsQuery(usersContext, Testing.GetAppSettings());
            results = query.Execute();
        });

        results.Any(p => p.OdsInstance.OdsInstanceId == odsInstanceContext1.OdsInstance.OdsInstanceId).ShouldBeTrue();
        results.Any(p => p.ContextKey == odsInstanceContext1.ContextKey).ShouldBeTrue();
        results.Any(p => p.ContextValue == odsInstanceContext1.ContextValue).ShouldBeTrue();

    }

    [Test]
    public void Should_Retreive_OdsInstancesContext_With_Offset_Limit()
    {
        var odsInstance = new OdsInstance
        {
            Name = "ODS Instance Name",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        var odsInstanceContext1 = new OdsInstanceContext
        {
            ContextKey = "contextKey",
            ContextValue = "contextValue",
            OdsInstance = odsInstance
        };

        var odsInstanceContext2 = new OdsInstanceContext
        {
            ContextKey = "contextKey2",
            ContextValue = "contextValue2",
            OdsInstance = odsInstance
        };

        Save(odsInstance, odsInstanceContext1, odsInstanceContext2);

        List<OdsInstanceContext> results = null;
        Transaction(usersContext =>
        {
            var query = new GetOdsInstanceContextsQuery(usersContext, Testing.GetAppSettings());
            results = query.Execute(new CommonQueryParams(1, 1));
            results.Count.ShouldBe(1);
        });

        results.Any(p => p.OdsInstance.OdsInstanceId == odsInstanceContext2.OdsInstance.OdsInstanceId).ShouldBeTrue();
        results.Any(p => p.ContextKey == odsInstanceContext2.ContextKey).ShouldBeTrue();
        results.Any(p => p.ContextValue == odsInstanceContext2.ContextValue).ShouldBeTrue();
    }
}



