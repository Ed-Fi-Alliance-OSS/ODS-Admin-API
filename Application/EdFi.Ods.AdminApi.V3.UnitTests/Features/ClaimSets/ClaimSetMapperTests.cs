// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using NUnit.Framework;
using Shouldly;
using SecurityAuthorizationStrategy = EdFi.Security.DataAccess.Models.AuthorizationStrategy;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ClaimSets;

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
    public void ToResourceClaim_MapsFlatResourceClaimModelValues()
    {
        var actions = new List<ResourceClaimAction>
        {
            new() { Name = "Read", Enabled = true }
        };
        var source = new ClaimSetResourceClaimModel
        {
            Name = "candidatePreparation",
            ClaimName = "http://ed-fi.org/identity/claims/candidatePreparation",
            ParentClaimName = "http://ed-fi.org/identity/claims/domains/educationStandards",
            Actions = actions,
            DefaultAuthorizationStrategies = new List<ClaimSetResourceClaimActionAuthStrategies>(),
            AuthorizationStrategyOverrides = new List<ClaimSetResourceClaimActionAuthStrategies>()
        };

        var model = ClaimSetMapper.ToResourceClaim(source);

        model.Name.ShouldBe(source.Name);
        model.ClaimName.ShouldBe(source.ClaimName);
        model.ParentClaimName.ShouldBe(source.ParentClaimName);
        model.Actions.ShouldBeSameAs(actions);
        model.Children.ShouldBeEmpty();
    }

    [Test]
    public void ToResourceClaimList_MapsEachEntryIndependently()
    {
        var source = new List<ClaimSetResourceClaimModel>
        {
            new() { Name = "educationStandards", ClaimName = "http://ed-fi.org/identity/claims/domains/educationStandards", ParentClaimName = null },
            new() { Name = "candidatePreparation", ClaimName = "http://ed-fi.org/identity/claims/candidatePreparation", ParentClaimName = "http://ed-fi.org/identity/claims/domains/educationStandards" }
        };

        var result = ClaimSetMapper.ToResourceClaimList(source);

        result.Count.ShouldBe(2);
        result[0].Children.ShouldBeEmpty();
        result[1].Children.ShouldBeEmpty();
        result[1].ParentClaimName.ShouldBe(source[1].ParentClaimName);
    }

    [Test]
    public void ToClaimSetResourceClaimModelList_FlattensNestedTreeWithParentClaimName()
    {
        var child = new ResourceClaim
        {
            Name = "candidatePreparation",
            ClaimName = "http://ed-fi.org/identity/claims/candidatePreparation",
            Actions = new List<ResourceClaimAction>(),
            Children = new List<ResourceClaim>()
        };
        var parent = new ResourceClaim
        {
            Name = "educationStandards",
            ClaimName = "http://ed-fi.org/identity/claims/domains/educationStandards",
            Actions = new List<ResourceClaimAction>(),
            Children = new List<ResourceClaim> { child }
        };

        var result = ClaimSetMapper.ToClaimSetResourceClaimModelList(new List<ResourceClaim> { parent });

        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("educationStandards");
        result[0].ParentClaimName.ShouldBeNull();
        result[1].Name.ShouldBe("candidatePreparation");
        result[1].ParentClaimName.ShouldBe("http://ed-fi.org/identity/claims/domains/educationStandards");
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
