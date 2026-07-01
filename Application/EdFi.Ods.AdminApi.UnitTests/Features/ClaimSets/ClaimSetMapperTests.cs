// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Features.ClaimSets;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using NUnit.Framework;
using Shouldly;
using SecurityAuthorizationStrategy = EdFi.Security.DataAccess.Models.AuthorizationStrategy;

namespace EdFi.Ods.AdminApi.UnitTests.Features.ClaimSets;

[TestFixture]
public class ClaimSetMapperTests
{
    [Test]
    public void ToModel_MapsClaimSetValuesAndSystemReservedFlag()
    {
        var source = new ClaimSet
        {
            Id = 7,
            Name = "ClaimSet",
            IsEditable = false
        };

        var model = ClaimSetMapper.ToModel(source);

        model.Id.ShouldBe(7);
        model.Name.ShouldBe("ClaimSet");
        model.IsSystemReserved.ShouldBeTrue();
    }

    [Test]
    public void ToModelList_MapsAllClaimSetsInOrder()
    {
        var source = new[]
        {
            new ClaimSet { Id = 1, Name = "First", IsEditable = true },
            new ClaimSet { Id = 2, Name = "Second", IsEditable = false }
        };

        var models = ClaimSetMapper.ToModelList(source);

        models.Select(x => x.Id).ShouldBe(new[] { 1, 2 });
        models.Select(x => x.Name).ShouldBe(new[] { "First", "Second" });
        models.Select(x => x.IsSystemReserved).ShouldBe(new[] { false, true });
    }

    [Test]
    public void ToClaimSetResourceClaimModel_MapsNestedResourceClaimValues()
    {
        var defaultStrategy = new ClaimSetResourceClaimActionAuthStrategies
        {
            ActionName = "Read",
            AuthorizationStrategies = new[] { new AuthorizationStrategy { AuthStrategyId = 3, AuthStrategyName = "Relationships" } }
        };
        var overrideStrategy = new ClaimSetResourceClaimActionAuthStrategies
        {
            ActionName = "Update",
            AuthorizationStrategies = new[] { new AuthorizationStrategy { AuthStrategyId = 4, AuthStrategyName = "Ownership" } }
        };
        var actions = new List<ResourceClaimAction>
        {
            new() { Name = "Read", Enabled = true }
        };
        var source = new ResourceClaim
        {
            Id = 10,
            Name = "Parent",
            Actions = actions,
            DefaultAuthorizationStrategiesForCRUD = new List<ClaimSetResourceClaimActionAuthStrategies> { defaultStrategy },
            AuthorizationStrategyOverridesForCRUD = new List<ClaimSetResourceClaimActionAuthStrategies> { overrideStrategy },
            Children = new List<ResourceClaim>
            {
                new() { Id = 11, Name = "Child" }
            }
        };

        var model = ClaimSetMapper.ToClaimSetResourceClaimModel(source);

        model.Id.ShouldBe(10);
        model.Name.ShouldBe("Parent");
        model.Actions.ShouldBeSameAs(actions);
        model.DefaultAuthorizationStrategiesForCRUD.Single().ShouldBeSameAs(defaultStrategy);
        model.AuthorizationStrategyOverridesForCRUD.Single().ShouldBeSameAs(overrideStrategy);
        model.Children.Single().Id.ShouldBe(11);
        model.Children.Single().Name.ShouldBe("Child");
    }

    [Test]
    public void ToResourceClaim_MapsNestedResourceClaimModelValues()
    {
        var actions = new List<ResourceClaimAction>
        {
            new() { Name = "Read", Enabled = true }
        };
        var source = new ClaimSetResourceClaimModel
        {
            Id = 20,
            Name = "Parent",
            Actions = actions,
            Children = new List<ClaimSetResourceClaimModel>
            {
                new() { Id = 21, Name = "Child" }
            }
        };

        var model = ClaimSetMapper.ToResourceClaim(source);

        model.Id.ShouldBe(20);
        model.Name.ShouldBe("Parent");
        model.Actions.ShouldBeSameAs(actions);
        model.Children.Single().Id.ShouldBe(21);
        model.Children.Single().Name.ShouldBe("Child");
    }

    [Test]
    public void ToEditResourceOnClaimSetModel_MapsClaimSetResourceAndActions()
    {
        var actions = new List<ResourceClaimAction>
        {
            new() { Name = "Read", Enabled = true }
        };
        var request = new TestResourceClaimOnClaimSetRequest
        {
            ClaimSetId = 30,
            ResourceClaimId = 40,
            ResourceClaimActions = actions
        };

        var model = ClaimSetMapper.ToEditResourceOnClaimSetModel(request);

        model.ClaimSetId.ShouldBe(30);
        model.ResourceClaim!.Id.ShouldBe(40);
        model.ResourceClaim.Actions.ShouldBeSameAs(actions);
    }

    [Test]
    public void ToAuthorizationStrategy_MapsSecurityAuthorizationStrategyAndInheritanceFlag()
    {
        var source = new SecurityAuthorizationStrategy
        {
            AuthorizationStrategyId = 50,
            AuthorizationStrategyName = "NamespaceBased"
        };

        var model = ClaimSetMapper.ToAuthorizationStrategy(source, true);

        model.AuthStrategyId.ShouldBe(50);
        model.AuthStrategyName.ShouldBe("NamespaceBased");
        model.IsInheritedFromParent.ShouldBeTrue();
    }

    private class TestResourceClaimOnClaimSetRequest : IResourceClaimOnClaimSetRequest
    {
        public int ClaimSetId { get; set; }

        public int ResourceClaimId { get; set; }

        public List<ResourceClaimAction> ResourceClaimActions { get; set; }
    }
}
