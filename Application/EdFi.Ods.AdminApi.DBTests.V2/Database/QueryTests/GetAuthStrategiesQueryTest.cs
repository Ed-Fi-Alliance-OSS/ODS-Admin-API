// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V2.Infrastructure;
using EdFi.Ods.AdminApi.V2.Infrastructure.Database.Queries;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.V2.Database.QueryTests;

[TestFixture]
public class GetAuthStrategiesQueryTest : PlatformSecurityContextTestBase
{
    protected override string ConnectionString => Testing.SecurityConnectionString;

    protected override SqlServerSecurityContext CreateDbContext()
    {
        return new SqlServerSecurityContext(Testing.GetDbContextOptions(ConnectionString));
    }

    [Test]
    public void Should_Retrieve_AuthStrategies()
    {
        var newAuthStrategy = new AuthorizationStrategy
        {
            AuthorizationStrategyName = "Test Auth S",
            DisplayName = "Test Auth Strategy",
        };

        Save(newAuthStrategy);

        Transaction(securityContext =>
        {
            var command = new GetAuthStrategiesQuery(securityContext, Testing.GetAppSettings());
            var allAuthStrategies = command.Execute();

            allAuthStrategies.ShouldNotBeEmpty();

            var authStrategy = allAuthStrategies.Single(v => v.AuthorizationStrategyId == newAuthStrategy.AuthorizationStrategyId);
            authStrategy.AuthorizationStrategyName.ShouldBe("Test Auth S");
            authStrategy.DisplayName.ShouldBe("Test Auth Strategy");
        });
    }

    [Test]
    public void Should_Retrieve_AuthStrategies_With_Offset_And_Limit()
    {
        var authStrategies = new AuthorizationStrategy[5];

        for (var index = 0; index < 5; index++)
        {
            authStrategies[index] = new AuthorizationStrategy
            {
                AuthorizationStrategyName = "Test Auth S " + (index + 1),
                DisplayName = "Test Auth Strategy" + (index + 1),
            };
        }

        Save(authStrategies);

        Transaction(securityContext =>
        {
            var command = new GetAuthStrategiesQuery(securityContext, Testing.GetAppSettings());

            var offset = 0;
            var limit = 2;

            var authStrategiesAfterOffset = command.Execute(new CommonQueryParams(offset, limit, null, null));

            authStrategiesAfterOffset.ShouldNotBeEmpty();
            authStrategiesAfterOffset.Count.ShouldBe(2);

            authStrategiesAfterOffset.ShouldContain(v => v.AuthorizationStrategyName == "Test Auth S 1");
            authStrategiesAfterOffset.ShouldContain(v => v.AuthorizationStrategyName == "Test Auth S 2");

            offset = 2;

            authStrategiesAfterOffset = command.Execute(new CommonQueryParams(offset, limit, null, null));

            authStrategiesAfterOffset.ShouldNotBeEmpty();
            authStrategiesAfterOffset.Count.ShouldBe(2);

            authStrategiesAfterOffset.ShouldContain(v => v.AuthorizationStrategyName == "Test Auth S 3");
            authStrategiesAfterOffset.ShouldContain(v => v.AuthorizationStrategyName == "Test Auth S 4");
            offset = 4;

            authStrategiesAfterOffset = command.Execute(new CommonQueryParams(offset, limit, null, null));

            authStrategiesAfterOffset.ShouldNotBeEmpty();
            authStrategiesAfterOffset.Count.ShouldBe(1);

            authStrategiesAfterOffset.ShouldContain(v => v.AuthorizationStrategyName == "Test Auth S 5");
        });
    }

}

