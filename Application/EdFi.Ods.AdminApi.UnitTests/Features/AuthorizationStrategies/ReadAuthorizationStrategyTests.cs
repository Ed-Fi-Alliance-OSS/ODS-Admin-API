// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Features.AuthorizationStrategies;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Security.DataAccess.Models;
using FakeItEasy;
using Microsoft.AspNetCore.Http.HttpResults;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.AuthorizationStrategies;

[TestFixture]
public class ReadAuthorizationStrategyTests
{
    [Test]
    public async Task GetAuthStrategies_ReturnsOkWithMappedStrategies()
    {
        var query = A.Fake<IGetAuthStrategiesQuery>();
        var strategies = new List<AuthorizationStrategy>
        {
            new()
            {
                AuthorizationStrategyId = 7,
                AuthorizationStrategyName = "NamespaceBased",
                DisplayName = "Namespace Based"
            }
        };
        var commonQueryParams = new CommonQueryParams();
        A.CallTo(() => query.Execute(commonQueryParams)).Returns(strategies);

        var result = await ReadAuthorizationStrategy.GetAuthStrategies(query, commonQueryParams);

        var ok = result.ShouldBeOfType<Ok<List<AuthorizationStrategyModel>>>();
        ok.Value.ShouldNotBeNull();
        ok.Value.Count.ShouldBe(1);
        ok.Value[0].AuthStrategyId.ShouldBe(7);
        ok.Value[0].AuthStrategyName.ShouldBe("NamespaceBased");
        ok.Value[0].DisplayName.ShouldBe("Namespace Based");
    }
}
