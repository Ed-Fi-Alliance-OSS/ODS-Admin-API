// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.QueryTests;

[TestFixture]
public class GetTenantEdOrgsByInstancesTests : AdminApiDbContextTestBase
{
    [Test]
    public void ShouldDistinguishLinkedAndUnlinkedDbInstances()
    {
        var linked = new DbInstance
        {
            Name = "Linked-Instance",
            OdsInstanceId = 999,
            Status = "Created",
            DatabaseTemplate = "Minimal",
            LastRefreshed = DateTime.UtcNow
        };
        var unlinked = new DbInstance
        {
            Name = "Unlinked-Instance",
            OdsInstanceId = null,
            Status = "PendingCreate",
            DatabaseTemplate = "Sample",
            LastRefreshed = DateTime.UtcNow
        };
        Save(linked, unlinked);

        Transaction(context =>
        {
            var query = new GetDbInstancesQuery(context, Testing.GetAppSettings());
            var allResults = query.Execute(new CommonQueryParams(0, null), null, null);

            var unlinkedResults = allResults.Where(d => d.OdsInstanceId == null).ToList();
            var linkedResults = allResults.Where(d => d.OdsInstanceId != null).ToList();

            unlinkedResults.Count.ShouldBe(1);
            unlinkedResults[0].Name.ShouldBe("Unlinked-Instance");
            unlinkedResults[0].Status.ShouldBe("PendingCreate");

            linkedResults.Count.ShouldBe(1);
            linkedResults[0].Name.ShouldBe("Linked-Instance");
            linkedResults[0].OdsInstanceId.ShouldBe(999);
        });
    }

    [Test]
    public void ShouldReturnAllDbInstances_WhenNoFiltersApplied()
    {
        Save(
            new DbInstance { Name = "A", OdsInstanceId = 1, Status = "Created", DatabaseTemplate = "Minimal", LastRefreshed = DateTime.UtcNow },
            new DbInstance { Name = "B", OdsInstanceId = null, Status = "PendingCreate", DatabaseTemplate = "Sample", LastRefreshed = DateTime.UtcNow },
            new DbInstance { Name = "C", OdsInstanceId = null, Status = "PendingCreate", DatabaseTemplate = "Minimal", LastRefreshed = DateTime.UtcNow }
        );

        Transaction(context =>
        {
            var query = new GetDbInstancesQuery(context, Testing.GetAppSettings());
            var results = query.Execute(new CommonQueryParams(0, null), null, null);
            results.Count.ShouldBe(3);
        });
    }

    [Test]
    public void ShouldReturnAllDbInstanceFields_ForLinkedInstance()
    {
        var dbInstance = new DbInstance
        {
            Name = "Fully-Linked",
            OdsInstanceId = 42,
            Status = "Created",
            DatabaseTemplate = "Minimal",
            DatabaseName = "EdFi_ODS_42",
            LastRefreshed = DateTime.UtcNow
        };
        Save(dbInstance);

        Transaction(context =>
        {
            var query = new GetDbInstancesQuery(context, Testing.GetAppSettings());
            var results = query.Execute(new CommonQueryParams(0, null), null, null);

            results.Count.ShouldBe(1);
            var result = results[0];
            result.OdsInstanceId.ShouldBe(42);
            result.Status.ShouldBe("Created");
            result.DatabaseTemplate.ShouldBe("Minimal");
            result.DatabaseName.ShouldBe("EdFi_ODS_42");
        });
    }

    [Test]
    public void ShouldReturnEmptyList_WhenNoDbInstancesExist()
    {
        Transaction(context =>
        {
            var query = new GetDbInstancesQuery(context, Testing.GetAppSettings());
            var results = query.Execute(new CommonQueryParams(0, null), null, null);
            results.ShouldBeEmpty();
        });
    }

    [Test]
    public void ShouldReturnMultipleUnlinkedInstances_InIdOrder()
    {
        Save(
            new DbInstance { Name = "Z-Unlinked", OdsInstanceId = null, Status = "PendingCreate", DatabaseTemplate = "Minimal", LastRefreshed = DateTime.UtcNow },
            new DbInstance { Name = "A-Unlinked", OdsInstanceId = null, Status = "PendingCreate", DatabaseTemplate = "Sample", LastRefreshed = DateTime.UtcNow }
        );

        Transaction(context =>
        {
            var query = new GetDbInstancesQuery(context, Testing.GetAppSettings());
            var results = query.Execute(new CommonQueryParams(0, null), null, null);
            var ids = results.Select(r => r.Id).ToList();
            ids.ShouldBe(ids.OrderBy(x => x).ToList());
        });
    }
}
