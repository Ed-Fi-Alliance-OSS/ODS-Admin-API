// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.DBTests.Database.QueryTests;

[TestFixture]
public class GetDbInstanceByIdQueryTests : AdminApiDbContextTestBase
{
    [Test]
    public void ShouldReturnNullForNonExistentId()
    {
        Transaction(context =>
        {
            var query = new GetDbInstanceByIdQuery(context);
            var result = query.Execute(0);
            result.ShouldBeNull();
        });
    }

    [Test]
    public void ShouldGetDbInstanceById()
    {
        var instance = new DbInstance
        {
            Name = "Test Instance",
            Status = "Pending",
            DatabaseTemplate = "Minimal",
            LastRefreshed = DateTime.UtcNow
        };
        Save(instance);

        Transaction(context =>
        {
            var query = new GetDbInstanceByIdQuery(context);
            var result = query.Execute(instance.Id);
            result.ShouldNotBeNull();
            result!.Id.ShouldBe(instance.Id);
            result.Name.ShouldBe("Test Instance");
            result.Status.ShouldBe("Pending");
            result.DatabaseTemplate.ShouldBe("Minimal");
        });
    }

    [Test]
    public void ShouldReturnNullWhenIdDoesNotMatch()
    {
        var instance = new DbInstance
        {
            Name = "Another Instance",
            Status = "Pending",
            DatabaseTemplate = "Sample",
            LastRefreshed = DateTime.UtcNow
        };
        Save(instance);

        Transaction(context =>
        {
            var query = new GetDbInstanceByIdQuery(context);
            var result = query.Execute(instance.Id + 9999);
            result.ShouldBeNull();
        });
    }
}



