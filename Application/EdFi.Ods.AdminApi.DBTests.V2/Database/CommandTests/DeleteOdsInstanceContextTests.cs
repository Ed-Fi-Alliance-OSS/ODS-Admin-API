// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V2.Infrastructure.Database.Commands;
using NUnit.Framework;
using Shouldly;
using System.Linq;

namespace EdFi.Ods.AdminApi.DBTests.V2.Database.CommandTests;

[TestFixture]
public class DeleteOdsInstanceContextTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldDeleteOdsInstanceContext()
    {
        var odsInstance = new OdsInstance
        {
            Name = "ODS Instance Name",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        var newOdsInstanceContext = new OdsInstanceContext()
        {
            ContextKey = "contextKey",
            ContextValue = "contextValue",
            OdsInstance = odsInstance
        };
        Save(newOdsInstanceContext);
        var odsInstanceContextId = newOdsInstanceContext.OdsInstanceContextId;

        Transaction(usersContext =>
        {
            var deleteOdsInstanceContextCommand = new DeleteOdsInstanceContextCommand(usersContext);
            deleteOdsInstanceContextCommand.Execute(odsInstanceContextId);
        });

        Transaction(usersContext => usersContext.OdsInstanceContexts.Where(v => v.OdsInstanceContextId == odsInstanceContextId).ToArray()).ShouldBeEmpty();
    }
}
