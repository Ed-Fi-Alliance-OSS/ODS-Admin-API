// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetDataStoreDerivativesQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetDataStoreDerivatives_{Guid.NewGuid()}")
            .Options);
    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });

    [Test]
    public void Execute_ReturnsList()
    {
        using var ctx = CreateContext();
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();
        ctx.OdsInstanceDerivatives.Add(new OdsInstanceDerivative { OdsInstance = ods, DerivativeType = "read-replica", ConnectionString = "cs-replica" });
        ctx.SaveChanges();
        var result = new GetDataStoreDerivativesQuery(ctx, DefaultOptions()).Execute(new CommonQueryParams(0, 25));
        result.Count.ShouldBe(1);
    }
}
