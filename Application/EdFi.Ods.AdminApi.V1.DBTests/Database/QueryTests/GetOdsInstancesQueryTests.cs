// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V1.DBTests.Database.QueryTests;

[TestFixture]
public class GetOdsInstancesQueryTests : PlatformUsersContextTestBase
{
    [Test]
    public void Should_retrieve_ods_instances()
    {
        var newOdsInstance = new OdsInstance
        {
            Name = "test ods instance",
            InstanceType = "test ods instance type",
            Status = "OK",
            Version = "1.0.0",
        };

        Save(newOdsInstance);

        Transaction(usersContext =>
        {
            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings());
            var allOdsInstances = command.Execute();

            allOdsInstances.ShouldNotBeEmpty();

            var odsInstance = allOdsInstances.Single(v => v.OdsInstanceId == newOdsInstance.OdsInstanceId);
            odsInstance.Name.ShouldBe("test ods instance");
            odsInstance.InstanceType.ShouldBe("test ods instance type");
        });
    }

    [Test]
    public void Should_retrieve_ods_instances_with_offset_and_limit()
    {
        var odsInstances = new OdsInstance[5];

        for (var odsInstanceIndex = 0; odsInstanceIndex < 5; odsInstanceIndex++)
        {
            odsInstances[odsInstanceIndex] = new OdsInstance
            {
                Name = $"test ods instance {odsInstanceIndex + 1}",
                InstanceType = "test ods instance type",
                Status = "OK",
                Version = "1.0.0",
            };
        }

        Save(odsInstances);

        Transaction(usersContext =>
        {
            var command = new GetOdsInstancesQuery(usersContext, Testing.GetAppSettings());
            var commonQueryParams = new CommonQueryParams(0, 2);

            var odsInstancesAfterOffset = command.Execute(commonQueryParams);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(2);

            odsInstancesAfterOffset.ShouldContain(v => v.Name == "test ods instance 1");
            odsInstancesAfterOffset.ShouldContain(v => v.Name == "test ods instance 2");

            commonQueryParams.Offset = 2;

            odsInstancesAfterOffset = command.Execute(commonQueryParams);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(2);

            odsInstancesAfterOffset.ShouldContain(v => v.Name == "test ods instance 3");
            odsInstancesAfterOffset.ShouldContain(v => v.Name == "test ods instance 4");
            commonQueryParams.Offset = 4;

            odsInstancesAfterOffset = command.Execute(commonQueryParams);

            odsInstancesAfterOffset.ShouldNotBeEmpty();
            odsInstancesAfterOffset.Count.ShouldBe(1);

            odsInstancesAfterOffset.ShouldContain(v => v.Name == "test ods instance 5");
        });
    }
}
