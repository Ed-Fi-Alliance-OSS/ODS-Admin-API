// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.V3.Database.QueryTests;

[TestFixture]
public class GetOdsInstanceContextByIdQueryTests : PlatformUsersContextTestBase
{
    [Test]
    public void Should_Retreive_OdsInstanceContext()
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

        OdsInstanceContext result = null;
        Transaction(usersContext =>
        {
            var query = new GetOdsInstanceContextByIdQuery(usersContext);
            result = query.Execute(odsInstanceContext1.OdsInstanceContextId);
            result.ShouldNotBeNull();
            result.OdsInstanceContextId.ShouldBe(odsInstanceContext1.OdsInstanceContextId);
            result.OdsInstance.OdsInstanceId.ShouldBe(odsInstanceContext1.OdsInstance.OdsInstanceId);
            result.ContextKey.ShouldBe(odsInstanceContext1.ContextKey);
            result.ContextValue.ShouldBe(odsInstanceContext1.ContextValue);
        });
    }

}

