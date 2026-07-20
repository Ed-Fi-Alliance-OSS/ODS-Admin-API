// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using EdFi.Ods.AdminApi.Features.AuthorizationStrategies;
using EdFi.Security.DataAccess.Models;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.AuthorizationStrategies;

[TestFixture]
public class AuthorizationStrategyMapperTests
{
    [Test]
    public void ToModel_MapsSecurityAuthorizationStrategyFields()
    {
        var source = new AuthorizationStrategy
        {
            AuthorizationStrategyId = 3,
            AuthorizationStrategyName = "RelationshipsWithStudentsOnly",
            DisplayName = "Relationships With Students Only"
        };

        var model = AuthorizationStrategyMapper.ToModel(source);

        model.AuthStrategyId.ShouldBe(3);
        model.AuthStrategyName.ShouldBe("RelationshipsWithStudentsOnly");
        model.DisplayName.ShouldBe("Relationships With Students Only");
    }

    [Test]
    public void ToModelList_PreservesSourceOrder()
    {
        var source = new List<AuthorizationStrategy>
        {
            new() { AuthorizationStrategyId = 1, AuthorizationStrategyName = "One" },
            new() { AuthorizationStrategyId = 2, AuthorizationStrategyName = "Two" }
        };

        var models = AuthorizationStrategyMapper.ToModelList(source);

        models.Count.ShouldBe(2);
        models[0].AuthStrategyId.ShouldBe(1);
        models[1].AuthStrategyId.ShouldBe(2);
    }
}
