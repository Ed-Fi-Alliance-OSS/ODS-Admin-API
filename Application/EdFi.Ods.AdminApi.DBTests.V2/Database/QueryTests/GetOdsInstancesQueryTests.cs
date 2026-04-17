// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V2.Infrastructure;
using EdFi.Ods.AdminApi.V2.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.V2.Database.QueryTests;

[TestFixture]
public class GetOdsInstancesQueryTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldGetAllInstances()
    {
        Transaction(usersContext =>
        {
            CreateMultiple(2);
            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings());
            var results = command.Execute();
            results.Count.ShouldBe(2);
        });
    }

    [Test]
    public void ShouldGetAllInstancesWithOffsetAndLimit()
    {
        Transaction(usersContext =>
        {
            CreateMultiple();
            var offset = 0;
            var limit = 2;

            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings());
            var odsInstancesAfterOffset = command.Execute(new CommonQueryParams(offset, limit), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(2);

            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 1");
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 2");

            offset = 2;

            odsInstancesAfterOffset = command.Execute(new CommonQueryParams(offset, limit), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(2);
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 3");
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 4");

            offset = 4;

            odsInstancesAfterOffset = command.Execute(new CommonQueryParams(offset, limit), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(1);
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 5");
        });
    }

    [Test]
    public void ShouldGetAllInstancesWithoutOffsetAndLimit()
    {
        Transaction(usersContext =>
        {
            CreateMultiple();

            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings());
            var odsInstancesAfterOffset = command.Execute(new CommonQueryParams(), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(5);

            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 1");
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 2");

            odsInstancesAfterOffset = command.Execute(new CommonQueryParams(), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(5);
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 3");
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 4");

            odsInstancesAfterOffset = command.Execute(new CommonQueryParams(), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(5);
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 5");
        });
    }
    [Test]
    public void ShouldGetAllInstancesWithoutLimit()
    {
        Transaction(usersContext =>
        {
            CreateMultiple();
            var offset = 0;

            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings());
            var odsInstancesAfterOffset = command.Execute(new CommonQueryParams(offset, null), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(5);

            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 1");
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 2");

            offset = 2;

            odsInstancesAfterOffset = command.Execute(new CommonQueryParams(offset, null), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(3);
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 3");
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 4");

            offset = 4;

            odsInstancesAfterOffset = command.Execute(new CommonQueryParams(offset, null), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(1);
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 5");
        });
    }

    [Test]
    public void ShouldGetAllInstancesWithoutOffset()
    {
        Transaction(usersContext =>
        {
            CreateMultiple();
            var limit = 2;

            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings());
            var odsInstancesAfterOffset = command.Execute(new CommonQueryParams(null, limit), null, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(2);

            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 1");
            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == "test ods instance 2");
        });
    }

    [Test]
    public void ShouldGetAllInstancesWithId()
    {
        Transaction(usersContext =>
        {
            var odsInstances = CreateMultiple();
            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings());
            var odsInstancesAfterOffset = command.Execute(new CommonQueryParams(), odsInstances[2].OdsInstanceId, null, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(1);

            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == odsInstances[2].Name);
        });
    }

    [Test]
    public void ShouldGetAllInstancesWithName()
    {
        Transaction(usersContext =>
        {
            var odsInstances = CreateMultiple();
            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings());
            var odsInstancesAfterOffset = command.Execute(new CommonQueryParams(), null, odsInstances[2].Name, null);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(1);

            odsInstancesAfterOffset.ShouldContain(odsI => odsI.Name == odsInstances[2].Name);
        });
    }

    private static OdsInstance[] CreateMultiple(int total = 5)
    {
        var odsInstances = new OdsInstance[total];

        for (var odsIndex = 0; odsIndex < total; odsIndex++)
        {
            odsInstances[odsIndex] = new OdsInstance
            {
                InstanceType = "test type",
                Name = $"test ods instance {odsIndex + 1}",
                ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
            };
        }
        Save(odsInstances);

        return odsInstances;
    }
}
