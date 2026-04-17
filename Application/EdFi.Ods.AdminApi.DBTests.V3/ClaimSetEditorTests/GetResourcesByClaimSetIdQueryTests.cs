// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Security.DataAccess.Models;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Action = EdFi.Security.DataAccess.Models.Action;
using ActionName = EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor.Action;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;
using ResourceClaim = EdFi.Security.DataAccess.Models.ResourceClaim;

namespace EdFi.Ods.AdminApi.DBTests.V3.ClaimSetEditorTests;

[TestFixture]
public class GetResourcesByClaimSetIdQueryTests : SecurityDataTestBase
{
    [Test]
    public void ShouldGetParentResourcesByClaimSetId()
    {
        var testClaimSets = SetupApplicationWithClaimSets().ToList();
        var testResourceClaims = SetupParentResourceClaims(testClaimSets, UniqueNameList("ParentRc", 1));

        foreach (var testClaimSet in testClaimSets)
        {
            var results = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId).ToArray();

            var testResourceClaimsForId =
                testResourceClaims.Where(x => x.ClaimSet.ClaimSetId == testClaimSet.ClaimSetId).Select(x => x.ResourceClaim).ToArray();
            results.Length.ShouldBe(testResourceClaimsForId.Length);
            results.Select(x => x.Name).ShouldBe(testResourceClaimsForId.Select(x => x.ResourceName), true);
            results.Select(x => x.Id).ShouldBe(testResourceClaimsForId.Select(x => x.ResourceClaimId), true);
            results.All(x => x.Actions.All(x => x.Name.Equals("Create") && x.Enabled)).ShouldBe(true);
        }
    }

    [Test]
    public void ShouldGetSingleResourceByClaimSetIdAndResourceId()
    {
        var testClaimSets = SetupApplicationWithClaimSets().ToList();
        var rcIds = UniqueNameList("ParentRc", 1);
        var testResourceClaims = SetupParentResourceClaims(testClaimSets, rcIds);

        foreach (var testClaimSet in testClaimSets)
        {
            var rcName = $"{rcIds.First()}{testClaimSet.ClaimSetName}";
            var testResourceClaim =
                testResourceClaims.Single(x => x.ClaimSet.ClaimSetId == testClaimSet.ClaimSetId && x.ResourceClaim.ResourceName == rcName).ResourceClaim;
            var result = SingleResourceClaimForClaimSet(testClaimSet.ClaimSetId, testResourceClaim.ResourceClaimId);

            result.Name.ShouldBe(testResourceClaim.ResourceName);
            result.Id.ShouldBe(testResourceClaim.ResourceClaimId);
            result.Actions.All(x => x.Name.Equals("Create") && x.Enabled).ShouldBe(true);
            result.Actions.All(x => x.Name.Equals("Read") && x.Enabled).ShouldBe(false);
            result.Actions.All(x => x.Name.Equals("Update") && x.Enabled).ShouldBe(false);
            result.Actions.All(x => x.Name.Equals("Delete") && x.Enabled).ShouldBe(false);
        }
    }

    [Test]
    public void ShouldGetParentResourcesWithChildrenByClaimSetId()
    {
        var testClaimSets = SetupApplicationWithClaimSets();
        var testResourceClaims = SetupParentResourceClaimsWithChildren(testClaimSets);

        using var securityContext = TestContext;
        foreach (var testClaimSet in testClaimSets)
        {
            var results = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId).ToArray();

            var testParentResourceClaimsForId =
                testResourceClaims.Where(x => x.ClaimSet.ClaimSetId == testClaimSet.ClaimSetId && x.ResourceClaim.ParentResourceClaim == null).Select(x => x.ResourceClaim).ToArray();

            results.Length.ShouldBe(testParentResourceClaimsForId.Length);
            results.Select(x => x.Name).ShouldBe(testParentResourceClaimsForId.Select(x => x.ResourceName), true);
            results.Select(x => x.Id).ShouldBe(testParentResourceClaimsForId.Select(x => x.ResourceClaimId), true);
            results.All(x => x.Actions.All(x => x.Name.Equals("Create") && x.Enabled)).ShouldBe(true);

            foreach (var testParentResourceClaim in testParentResourceClaimsForId)
            {
                var testChildren = securityContext.ResourceClaims.Where(x =>
                    x.ParentResourceClaimId == testParentResourceClaim.ResourceClaimId).ToList();
                var parentResult = results.First(x => x.Id == testParentResourceClaim.ResourceClaimId);
                parentResult.Children.Select(x => x.Name).ShouldBe(testChildren.Select(x => x.ResourceName), true);
                parentResult.Children.Select(x => x.Id).ShouldBe(testChildren.Select(x => x.ResourceClaimId), true);
                parentResult.Children.All(x => x.Actions.All(x => x.Name.Equals("Create") && x.Enabled)).ShouldBe(true);
            }

        }
    }

    [Test]
    public void ShouldGetDefaultAuthorizationStrategiesForParentResourcesByClaimSetId()
    {
        var testClaimSet = new ClaimSet
        {
            ClaimSetName = "TestClaimSet",
        };
        Save(testClaimSet);

        var appAuthorizationStrategies = SetupApplicationAuthorizationStrategies().ToList();
        var testResourceClaims = SetupParentResourceClaims(new List<ClaimSet> { testClaimSet }, UniqueNameList("ParentRc", 3));
        var testAuthStrategies = SetupResourcesWithDefaultAuthorizationStrategies(appAuthorizationStrategies, testResourceClaims.ToList());

        var results = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId).ToArray();
        results.Select(x => x.DefaultAuthorizationStrategiesForCRUD[0].AuthorizationStrategies.ToList().First().AuthStrategyName).ShouldBe(testAuthStrategies.Select(x => x.AuthorizationStrategies.Single()
                        .AuthorizationStrategy.AuthorizationStrategyName), true);

    }

    [Test]
    public void ShouldGetDefaultAuthorizationStrategiesForSingleResourcesByClaimSetIdAndResourceId()
    {
        var testClaimSet = new ClaimSet
        {
            ClaimSetName = "TestClaimSet",
        };
        Save(testClaimSet);

        var appAuthorizationStrategies = SetupApplicationAuthorizationStrategies().ToList();
        var rcIds = UniqueNameList("Parent", 1);
        var testResourceClaims = SetupParentResourceClaims(new List<ClaimSet> { testClaimSet }, rcIds);
        var testAuthStrategies = SetupResourcesWithDefaultAuthorizationStrategies(appAuthorizationStrategies, testResourceClaims.ToList());

        var rcName = $"{rcIds.First()}{testClaimSet.ClaimSetName}";
        var testResourceClaim =
            testResourceClaims.Single(x => x.ClaimSet.ClaimSetId == testClaimSet.ClaimSetId && x.ResourceClaim.ResourceName == rcName).ResourceClaim;
        var testAuthStrategy = testAuthStrategies.Single(x =>
            x.ResourceClaim.ResourceClaimId == testResourceClaim.ResourceClaimId && x.Action.ActionName == ActionName.Create.Value)
        .AuthorizationStrategies.Single().AuthorizationStrategy;

        var result = SingleResourceClaimForClaimSet(testClaimSet.ClaimSetId, testResourceClaim.ResourceClaimId);

        result.Name.ShouldBe(testResourceClaim.ResourceName);
        result.Id.ShouldBe(testResourceClaim.ResourceClaimId);
        result.Actions.All(x => x.Name.Equals("Create") && x.Enabled).ShouldBe(true);
        result.Actions.All(x => x.Name.Equals("Read") && x.Enabled).ShouldBe(false);
        result.Actions.All(x => x.Name.Equals("Update") && x.Enabled).ShouldBe(false);
        result.Actions.All(x => x.Name.Equals("Delete") && x.Enabled).ShouldBe(false);
        result.DefaultAuthorizationStrategiesForCRUD[0].AuthorizationStrategies.ToList().First().AuthStrategyName.ShouldBe(testAuthStrategy.DisplayName);

    }

    [Test]
    public void ShouldGetDefaultAuthorizationStrategiesForParentResourcesWithChildrenByClaimSetId()
    {
        var testClaimSet = new ClaimSet
        {
            ClaimSetName = "TestClaimSet",
        };
        Save(testClaimSet);

        var appAuthorizationStrategies = SetupApplicationAuthorizationStrategies().ToList();

        var testResourceClaims = SetupParentResourceClaimsWithChildren(new List<ClaimSet> { testClaimSet });
        var testAuthStrategies = SetupResourcesWithDefaultAuthorizationStrategies(appAuthorizationStrategies, testResourceClaims.ToList());

        var results = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId).ToArray();

        var testParentResourceClaimsForId =
            testResourceClaims
                .Where(x => x.ClaimSet.ClaimSetId == testClaimSet.ClaimSetId &&
                            x.ResourceClaim.ParentResourceClaim == null).Select(x => x.ResourceClaim).ToArray();

        var testAuthStrategiesForParents =
            testAuthStrategies.Where(x => x.ResourceClaim.ParentResourceClaim == null);

        results.Select(x => x.DefaultAuthorizationStrategiesForCRUD[0].AuthorizationStrategies.ToList().First().AuthStrategyName).ShouldBe(testAuthStrategiesForParents.Select(x =>
        x.AuthorizationStrategies.Single().AuthorizationStrategy.AuthorizationStrategyName), true);

        foreach (var testParentResourceClaim in testParentResourceClaimsForId)
        {
            var parentResult = results.First(x => x.Id == testParentResourceClaim.ResourceClaimId);
            var testAuthStrategiesForChildren =
                testAuthStrategies.Where(x =>
                    x.ResourceClaim.ParentResourceClaimId == testParentResourceClaim.ResourceClaimId);
            parentResult.Children.Select(x => x.DefaultAuthorizationStrategiesForCRUD[0].AuthorizationStrategies.ToList().First().AuthStrategyName).ShouldBe(testAuthStrategiesForChildren.Select(x => x.AuthorizationStrategies.Single()
            .AuthorizationStrategy.AuthorizationStrategyName), true);
        }
    }

    private IReadOnlyCollection<ClaimSet> SetupApplicationWithClaimSets(int claimSetCount = 5)
    {
        var testClaimSetNames = Enumerable.Range(1, claimSetCount)
            .Select((x, index) => $"TestClaimSetName{index:N}")
            .ToArray();

        var testClaimSets = testClaimSetNames
            .Select(x => new ClaimSet
            {
                ClaimSetName = x,
            })
            .ToArray();

        Save(testClaimSets.Cast<object>().ToArray());

        return testClaimSets;
    }

    private IReadOnlyCollection<ClaimSetResourceClaimAction> SetupParentResourceClaims(IEnumerable<ClaimSet> testClaimSets, IList<string> resouceClaimsIds)
    {
        var claimSetResourceClaims = new List<ClaimSetResourceClaimAction>();
        foreach (var claimSet in testClaimSets)
        {
            foreach (var index in resouceClaimsIds)
            {
                var rcName = $"{index}{claimSet.ClaimSetName}";
                var resourceClaim = new ResourceClaim
                {
                    ClaimName = rcName,
                    ResourceName = rcName,
                };
                var action = new Action
                {
                    ActionName = ActionName.Create.Value,
                    ActionUri = "create"
                };
                var claimSetResourceClaim = new ClaimSetResourceClaimAction
                {
                    ResourceClaim = resourceClaim, Action = action, ClaimSet = claimSet
                };
                claimSetResourceClaims.Add(claimSetResourceClaim);
            }
        }

        Save(claimSetResourceClaims.Cast<object>().ToArray());

        return claimSetResourceClaims;
    }

    private IReadOnlyCollection<ClaimSetResourceClaimAction> SetupParentResourceClaimsWithChildren(IEnumerable<ClaimSet> testClaimSets, int resourceClaimCount = 5, int childResourceClaimCount = 1)
    {
        var parentResourceClaims = new List<ResourceClaim>();
        var childResourceClaims = new List<ResourceClaim>();
        foreach (var parentIndex in Enumerable.Range(1, resourceClaimCount))
        {
            var resourceClaim = new ResourceClaim
            {
                ClaimName = $"TestParentResourceClaim{parentIndex:N}",
                ResourceName = $"TestParentResourceClaim{parentIndex:N}",
            };
            parentResourceClaims.Add(resourceClaim);

            childResourceClaims.AddRange(Enumerable.Range(1, childResourceClaimCount)
                .Select(childIndex => new ResourceClaim
                {
                    ClaimName = $"TestChildResourceClaim{resourceClaim.ClaimName}",
                    ResourceName = $"TestChildResourceClaim{resourceClaim.ClaimName}",
                    ParentResourceClaim = resourceClaim,
                    ParentResourceClaimId = resourceClaim.ResourceClaimId
                }));
        }

        Save(parentResourceClaims.Cast<object>().ToArray());
        Save(childResourceClaims.Cast<object>().ToArray());

        var claimSetResourceClaims = new List<ClaimSetResourceClaimAction>();
        var claimSets = testClaimSets.ToList();
        foreach (var claimSet in claimSets)
        {
            foreach (var index in Enumerable.Range(1, childResourceClaimCount))
            {
                var action = new Action
                {
                    ActionName = ActionName.Create.Value,
                    ActionUri = "create"
                };
                var claimSetResourceClaim = new ClaimSetResourceClaimAction
                {
                    ResourceClaim = childResourceClaims[index - 1],
                    Action = action,
                    ClaimSet = claimSet
                };
                claimSetResourceClaims.Add(claimSetResourceClaim);
            }
        }

        Save(claimSetResourceClaims.Cast<object>().ToArray());

        claimSetResourceClaims = new List<ClaimSetResourceClaimAction>();
        foreach (var claimSet in claimSets)
        {
            foreach (var index in Enumerable.Range(1, resourceClaimCount))
            {
                var parentResource = parentResourceClaims[index - 1];
                var action = new Action
                {
                    ActionName = ActionName.Create.Value,
                    ActionUri = "create"
                };
                var claimSetResourceClaim = new ClaimSetResourceClaimAction
                {
                    ResourceClaim = parentResource,
                    Action = action,
                    ClaimSet = claimSet
                };
                claimSetResourceClaims.Add(claimSetResourceClaim);
                var childResources = childResourceClaims
                    .Where(x => x.ParentResourceClaimId == parentResource.ResourceClaimId).Select(x =>
                        new ClaimSetResourceClaimAction
                        {
                            ResourceClaim = x,
                            Action = action,
                            ClaimSet = claimSet
                        }).ToArray();
                claimSetResourceClaims.AddRange(childResources);
            }
        }

        Save(claimSetResourceClaims.Cast<object>().ToArray());

        return claimSetResourceClaims;
    }
}
