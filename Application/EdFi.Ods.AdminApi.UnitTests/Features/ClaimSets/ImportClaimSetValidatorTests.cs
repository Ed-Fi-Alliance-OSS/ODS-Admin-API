// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Features;
using EdFi.Ods.AdminApi.Features.ClaimSets;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;
using SecurityAction = EdFi.Security.DataAccess.Models.Action;

namespace EdFi.Ods.AdminApi.UnitTests.Features.ClaimSets;

[TestFixture]
public class ImportClaimSetValidatorTests
{
    [Test]
    public void Should_Have_Error_When_Name_Already_Exists()
    {
        var validator = CreateValidator(claimSets: new[] { new ClaimSet { Id = 1, Name = "Existing" } });
        var request = new ImportClaimSet.ImportClaimSetRequest
        {
            Name = "Existing",
            ResourceClaims = new List<ClaimSetResourceClaimModel>()
        };

        var result = validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.Name)
            && x.ErrorMessage == FeatureConstants.ClaimSetAlreadyExistsMessage)
            .ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_ResourceClaim_Actions_Are_Empty()
    {
        var validator = CreateValidator(
            resourceClaims: new[] { new ResourceClaim { Id = 1, Name = "Student" } },
            actions: new[] { "Read" });
        var request = new ImportClaimSet.ImportClaimSetRequest
        {
            Name = "NewClaimSet",
            ResourceClaims = new List<ClaimSetResourceClaimModel>
            {
                new() { Id = 1, Name = "Student", Actions = new List<ResourceClaimAction>() }
            }
        };

        var result = validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.ResourceClaims)
            && x.ErrorMessage == "Actions can not be empty.")
            .ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_ResourceClaim_Action_Is_Not_Valid()
    {
        var validator = CreateValidator(
            resourceClaims: new[] { new ResourceClaim { Id = 1, Name = "Student" } },
            actions: new[] { "Read" });
        var request = new ImportClaimSet.ImportClaimSetRequest
        {
            Name = "NewClaimSet",
            ResourceClaims = new List<ClaimSetResourceClaimModel>
            {
                new()
                {
                    Id = 1,
                    Name = "Student",
                    Actions = new List<ResourceClaimAction> { new() { Name = "InvalidAction", Enabled = true } }
                }
            }
        };

        var result = validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.ResourceClaims)
            && x.ErrorMessage == "InvalidAction is not a valid action.")
            .ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_Default_Authorization_Strategy_Is_Not_Valid()
    {
        var validator = CreateValidator(
            resourceClaims: new[] { new ResourceClaim { Id = 1, Name = "Student" } },
            actions: new[] { "Read" },
            authorizationStrategies: new[] { "Relationships" });
        var request = new ImportClaimSet.ImportClaimSetRequest
        {
            Name = "NewClaimSet",
            ResourceClaims = new List<ClaimSetResourceClaimModel>
            {
                new()
                {
                    Id = 1,
                    Name = "Student",
                    Actions = new List<ResourceClaimAction> { new() { Name = "Read", Enabled = true } },
                    DefaultAuthorizationStrategiesForCRUD = new List<ClaimSetResourceClaimActionAuthStrategies>
                    {
                        new()
                        {
                            ActionName = "Read",
                            AuthorizationStrategies = new[]
                            {
                                new AuthorizationStrategy { AuthStrategyName = "MissingStrategy" }
                            }
                        }
                    }
                }
            }
        };

        var result = validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.ResourceClaims)
            && x.ErrorMessage.Contains("Authorization strategy: 'MissingStrategy'."))
            .ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_Child_ResourceClaim_Is_Added_To_Wrong_Parent()
    {
        var validator = CreateValidator(
            resourceClaims: new[]
            {
                new ResourceClaim { Id = 1, Name = "Student" },
                new ResourceClaim { Id = 2, Name = "StudentSectionAssociation", ParentId = 99, ParentName = "Section" }
            },
            actions: new[] { "Read" });
        var request = new ImportClaimSet.ImportClaimSetRequest
        {
            Name = "NewClaimSet",
            ResourceClaims = new List<ClaimSetResourceClaimModel>
            {
                new()
                {
                    Id = 1,
                    Name = "Student",
                    Actions = new List<ResourceClaimAction> { new() { Name = "Read", Enabled = true } },
                    Children = new List<ClaimSetResourceClaimModel>
                    {
                        new()
                        {
                            Id = 2,
                            Name = "StudentSectionAssociation",
                            Actions = new List<ResourceClaimAction> { new() { Name = "Read", Enabled = true } }
                        }
                    }
                }
            }
        };

        var result = validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.ResourceClaims)
            && x.ErrorMessage.Contains("added to the wrong parent resource"))
            .ShouldBeTrue();
    }

    private static ImportClaimSet.Validator CreateValidator(
        IEnumerable<ClaimSet> claimSets = null,
        IEnumerable<ResourceClaim> resourceClaims = null,
        IEnumerable<string> actions = null,
        IEnumerable<string> authorizationStrategies = null)
    {
        var fakeGetAllClaimSetsQuery = A.Fake<IGetAllClaimSetsQuery>();
        A.CallTo(() => fakeGetAllClaimSetsQuery.Execute())
            .Returns((claimSets ?? Enumerable.Empty<ClaimSet>()).ToList());

        var fakeGetResourceClaimsAsFlatListQuery = A.Fake<IGetResourceClaimsAsFlatListQuery>();
        A.CallTo(() => fakeGetResourceClaimsAsFlatListQuery.Execute())
            .Returns((resourceClaims ?? Enumerable.Empty<ResourceClaim>()).ToList());

        var fakeGetAllAuthorizationStrategiesQuery = A.Fake<IGetAllAuthorizationStrategiesQuery>();
        A.CallTo(() => fakeGetAllAuthorizationStrategiesQuery.Execute())
            .Returns((authorizationStrategies ?? Enumerable.Empty<string>())
                .Select((name, index) => new AuthorizationStrategy
                {
                    AuthStrategyId = index + 1,
                    AuthStrategyName = name
                })
                .ToList());

        var fakeGetAllActionsQuery = A.Fake<IGetAllActionsQuery>();
        A.CallTo(() => fakeGetAllActionsQuery.Execute())
            .Returns((actions ?? Enumerable.Empty<string>())
                .Select((name, index) => new SecurityAction
                {
                    ActionId = index + 1,
                    ActionName = name
                })
                .ToList());

        return new ImportClaimSet.Validator(
            fakeGetAllClaimSetsQuery,
            fakeGetResourceClaimsAsFlatListQuery,
            fakeGetAllAuthorizationStrategiesQuery,
            fakeGetAllActionsQuery);
    }
}
