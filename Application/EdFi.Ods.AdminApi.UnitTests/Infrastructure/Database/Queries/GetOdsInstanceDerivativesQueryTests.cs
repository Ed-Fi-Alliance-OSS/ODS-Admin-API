// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetOdsInstanceDerivativesQueryTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"GetOdsInstanceDerivativesQueryTests_{Guid.NewGuid()}")
            .Options);

    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });

    [Test]
    public void Execute_ReturnsAllDerivativesOrderedByDerivativeType()
    {
        using var context = CreateContext();
        context.OdsInstanceDerivatives.AddRange(
            new OdsInstanceDerivative
            {
                OdsInstance = new OdsInstance { Name = "Instance One", InstanceType = "type", ConnectionString = "cs1" },
                DerivativeType = "zeta",
                ConnectionString = "derivative-zeta"
            },
            new OdsInstanceDerivative
            {
                OdsInstance = new OdsInstance { Name = "Instance Two", InstanceType = "type", ConnectionString = "cs2" },
                DerivativeType = "alpha",
                ConnectionString = "derivative-alpha"
            });
        context.SaveChanges();

        var query = new GetOdsInstanceDerivativesQuery(context, DefaultOptions());

        var result = query.Execute();

        result.Select(x => x.DerivativeType).ShouldBe(["alpha", "zeta"]);
    }

    [Test]
    public void Execute_WithCommonQueryParams_AppliesPagination()
    {
        using var context = CreateContext();
        context.OdsInstanceDerivatives.AddRange(
            new OdsInstanceDerivative
            {
                OdsInstance = new OdsInstance { Name = "Instance One", InstanceType = "type", ConnectionString = "cs1" },
                DerivativeType = "alpha",
                ConnectionString = "derivative-alpha"
            },
            new OdsInstanceDerivative
            {
                OdsInstance = new OdsInstance { Name = "Instance Two", InstanceType = "type", ConnectionString = "cs2" },
                DerivativeType = "beta",
                ConnectionString = "derivative-beta"
            });
        context.SaveChanges();

        var query = new GetOdsInstanceDerivativesQuery(context, DefaultOptions());

        var result = query.Execute(new CommonQueryParams(0, 1));

        result.Count.ShouldBe(1);
        result.Single().DerivativeType.ShouldBe("alpha");
    }
}
