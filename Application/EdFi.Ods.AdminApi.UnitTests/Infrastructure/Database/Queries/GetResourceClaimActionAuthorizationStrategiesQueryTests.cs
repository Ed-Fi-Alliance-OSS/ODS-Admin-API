// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using Action = EdFi.Security.DataAccess.Models.Action;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class GetResourceClaimActionAuthorizationStrategiesQueryTests
{
    private static SqlServerSecurityContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerSecurityContext>()
            .UseInMemoryDatabase(databaseName: $"GetRCAAStrategies_{Guid.NewGuid()}")
            .Options);

    private static IOptions<AppSettings> DefaultOptions() =>
        Options.Create(new AppSettings { DatabaseEngine = "Postgres", DefaultPageSizeLimit = 25 });

    [Test]
    public void Execute_ReturnsGroupedResourceClaimActionAuthStrategies()
    {
        using var ctx = CreateContext();
        var rc = new ResourceClaim { ResourceName = "schools", ClaimName = "schools" };
        ctx.ResourceClaims.Add(rc);
        var action = new Action { ActionName = "Read", ActionUri = "uri/read" };
        ctx.Actions.Add(action);
        var strat = new AuthorizationStrategy { AuthorizationStrategyName = "NoFurtherAuth", DisplayName = "NoFurtherAuth" };
        ctx.AuthorizationStrategies.Add(strat);
        ctx.SaveChanges();

        var rca = new ResourceClaimAction { ResourceClaim = rc, Action = action };
        ctx.ResourceClaimActions.Add(rca);
        ctx.SaveChanges();

        ctx.ResourceClaimActionAuthorizationStrategies.Add(new ResourceClaimActionAuthorizationStrategies
        {
            ResourceClaimAction = rca,
            AuthorizationStrategy = strat
        });
        ctx.SaveChanges();

        var result = new GetResourceClaimActionAuthorizationStrategiesQuery(ctx, DefaultOptions())
            .Execute(new CommonQueryParams(0, 25), null);

        result.Count.ShouldBe(1);
        result[0].ResourceName.ShouldBe("schools");
        result[0].AuthorizationStrategiesForActions.ShouldNotBeEmpty();
    }

    [Test]
    public void Execute_WithResourceNameFilter_ReturnsOnlyMatchingResource()
    {
        using var ctx = CreateContext();
        var rc1 = new ResourceClaim { ResourceName = "schools", ClaimName = "schools" };
        var rc2 = new ResourceClaim { ResourceName = "students", ClaimName = "students" };
        ctx.ResourceClaims.AddRange(rc1, rc2);
        var action = new Action { ActionName = "Read", ActionUri = "uri/read" };
        ctx.Actions.Add(action);
        var strat = new AuthorizationStrategy { AuthorizationStrategyName = "NoFurtherAuth", DisplayName = "NoFurtherAuth" };
        ctx.AuthorizationStrategies.Add(strat);
        ctx.SaveChanges();

        var rca1 = new ResourceClaimAction { ResourceClaim = rc1, Action = action };
        var rca2 = new ResourceClaimAction { ResourceClaim = rc2, Action = action };
        ctx.ResourceClaimActions.AddRange(rca1, rca2);
        ctx.SaveChanges();

        ctx.ResourceClaimActionAuthorizationStrategies.Add(new ResourceClaimActionAuthorizationStrategies { ResourceClaimAction = rca1, AuthorizationStrategy = strat });
        ctx.ResourceClaimActionAuthorizationStrategies.Add(new ResourceClaimActionAuthorizationStrategies { ResourceClaimAction = rca2, AuthorizationStrategy = strat });
        ctx.SaveChanges();

        var result = new GetResourceClaimActionAuthorizationStrategiesQuery(ctx, DefaultOptions())
            .Execute(new CommonQueryParams(0, 25), "schools");

        result.Count.ShouldBe(1);
        result[0].ResourceName.ShouldBe("schools");
    }
}
