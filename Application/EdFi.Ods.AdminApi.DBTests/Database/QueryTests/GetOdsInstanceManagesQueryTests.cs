// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.QueryTests;

[TestFixture]
public class GetOdsInstanceManagesQueryTests : AdminApiDbContextTestBase
{
    private static IEnumerable<string> AllStatuses =>
        Enum.GetValues<OdsInstanceManageStatus>().Select(status => status.ToString());

    [Test]
    public void ShouldRetrieveOdsInstanceManages()
    {
        var instance = new OdsInstanceManage
        {
            Name = "Test Instance",
            Status = "Pending",
            DatabaseTemplate = "Minimal",
            LastRefreshed = DateTime.UtcNow
        };
        Save(instance);

        Transaction(context =>
        {
            var query = new GetOdsInstanceManagesQuery(context, Testing.GetAppSettings());
            var results = query.Execute(new CommonQueryParams(), null, null);
            results.ShouldNotBeEmpty();
            results.ShouldContain(d => d.Id == instance.Id && d.Name == "Test Instance");
        });
    }

    [Test]
    public void ShouldReturnEmptyListWhenNoOdsInstanceManages()
    {
        Transaction(context =>
        {
            var query = new GetOdsInstanceManagesQuery(context, Testing.GetAppSettings());
            var results = query.Execute(new CommonQueryParams(), null, null);
            results.ShouldBeEmpty();
        });
    }

    [Test]
    public void ShouldFilterByName()
    {
        Save(
            new OdsInstanceManage { Name = "Instance Alpha", Status = "Pending", DatabaseTemplate = "Minimal", LastRefreshed = DateTime.UtcNow },
            new OdsInstanceManage { Name = "Instance Beta", Status = "Pending", DatabaseTemplate = "Sample", LastRefreshed = DateTime.UtcNow }
        );

        Transaction(context =>
        {
            var query = new GetOdsInstanceManagesQuery(context, Testing.GetAppSettings());
            var results = query.Execute(new CommonQueryParams(), null, "Instance Alpha");
            results.Count.ShouldBe(1);
            results.Single().Name.ShouldBe("Instance Alpha");
        });
    }

    [Test]
    public void ShouldFilterById()
    {
        var instance1 = new OdsInstanceManage { Name = "Instance 1", Status = "Pending", DatabaseTemplate = "Minimal", LastRefreshed = DateTime.UtcNow };
        var instance2 = new OdsInstanceManage { Name = "Instance 2", Status = "Pending", DatabaseTemplate = "Sample", LastRefreshed = DateTime.UtcNow };
        Save(instance1, instance2);

        Transaction(context =>
        {
            var query = new GetOdsInstanceManagesQuery(context, Testing.GetAppSettings());
            var results = query.Execute(new CommonQueryParams(), instance1.Id, null);
            results.Count.ShouldBe(1);
            results.Single().Id.ShouldBe(instance1.Id);
            results.Single().Name.ShouldBe("Instance 1");
        });
    }

    [Test]
    public void ShouldRetrieveOdsInstanceManagesWithOffsetAndLimit()
    {
        for (var i = 1; i <= 5; i++)
        {
            Save(new OdsInstanceManage
            {
                Name = $"Instance {i:D2}",
                Status = "Pending",
                DatabaseTemplate = "Minimal",
                LastRefreshed = DateTime.UtcNow
            });
        }

        Transaction(context =>
        {
            var query = new GetOdsInstanceManagesQuery(context, Testing.GetAppSettings());

            var results = query.Execute(new CommonQueryParams(0, 2), null, null);
            results.Count.ShouldBe(2);

            results = query.Execute(new CommonQueryParams(2, 2), null, null);
            results.Count.ShouldBe(2);

            results = query.Execute(new CommonQueryParams(4, 2), null, null);
            results.Count.ShouldBe(1);
        });
    }

    [Test]
    public void ShouldRetrieveAllOdsInstanceManagesWithoutLimit()
    {
        for (var i = 1; i <= 5; i++)
        {
            Save(new OdsInstanceManage
            {
                Name = $"Instance {i:D2}",
                Status = "Pending",
                DatabaseTemplate = "Minimal",
                LastRefreshed = DateTime.UtcNow
            });
        }

        Transaction(context =>
        {
            var query = new GetOdsInstanceManagesQuery(context, Testing.GetAppSettings());
            var results = query.Execute(new CommonQueryParams(0, null), null, null);
            results.Count.ShouldBe(5);
        });
    }

    [Test]
    public void ShouldRetrieveOdsInstanceManagesOrderedById()
    {
        Save(
            new OdsInstanceManage { Name = "Instance C", Status = "Pending", DatabaseTemplate = "Minimal", LastRefreshed = DateTime.UtcNow },
            new OdsInstanceManage { Name = "Instance A", Status = "Pending", DatabaseTemplate = "Minimal", LastRefreshed = DateTime.UtcNow },
            new OdsInstanceManage { Name = "Instance B", Status = "Pending", DatabaseTemplate = "Minimal", LastRefreshed = DateTime.UtcNow }
        );

        Transaction(context =>
        {
            var query = new GetOdsInstanceManagesQuery(context, Testing.GetAppSettings());
            var results = query.Execute(new CommonQueryParams(), null, null);
            var ids = results.Select(r => r.Id).ToList();
            ids.ShouldBe(ids.OrderBy(x => x).ToList());
        });
    }

    [Test]
    [TestCaseSource(nameof(AllStatuses))]
    public void ShouldIncludeOdsInstanceManages_ForAllStatuses_WhenNoFiltersApplied(string status)
    {
        Save(
            new OdsInstanceManage { Name = $"Status-{status}", OdsInstanceId = 111, Status = status, DatabaseTemplate = "Minimal", LastRefreshed = DateTime.UtcNow },
            new OdsInstanceManage { Name = "Active Instance", OdsInstanceId = 222, Status = "Created", DatabaseTemplate = "Minimal", LastRefreshed = DateTime.UtcNow }
        );

        Transaction(context =>
        {
            var query = new GetOdsInstanceManagesQuery(context, Testing.GetAppSettings());
            var results = query.Execute(new CommonQueryParams(0, null), null, null);
            results.Count.ShouldBe(2);
            results.ShouldContain(r => r.Name == $"Status-{status}" && r.Status == status);
        });
    }
}
