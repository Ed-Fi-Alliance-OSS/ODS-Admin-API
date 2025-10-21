// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.V1.Security.DataAccess.Models;
using NUnit.Framework;
using Shouldly;
using Action = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.Action;
using ActionName = EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor.Action;
using Application = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.Application;
using ClaimSet = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.ClaimSet;
using ResourceClaim = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.ResourceClaim;

namespace EdFi.Ods.AdminApi.V1.DBTests.ClaimSetEditorTests;

[TestFixture]
public class GetResourcesByClaimSetIdQueryV6SerivceTests : SecurityDataTestBase
{
    [Test]
    public void ShouldGetParentResourcesByClaimSetId()
    {
        var testApplication = new Application
        {
            ApplicationName = "TestApplicationName"
        };

        Save(testApplication);
        var testClaimSets = SetupApplicationWithClaimSets(testApplication).ToList();
        var testResourceClaims = SetupParentResourceClaims(testClaimSets, testApplication, UniqueNameList("ParentRc", 1));

        foreach (var testClaimSet in testClaimSets)
        {
            var results = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId).ToArray();

            var testResourceClaimsForId =
                testResourceClaims.Where(x => x.ClaimSet.ClaimSetId == testClaimSet.ClaimSetId).Select(x => x.ResourceClaim).ToArray();
            results.Length.ShouldBe(testResourceClaimsForId.Length);
            results.Select(x => x.Name).ShouldBe(testResourceClaimsForId.Select(x => x.ResourceName), true);
            results.Select(x => x.Id).ShouldBe(testResourceClaimsForId.Select(x => x.ResourceClaimId), true);
            results.All(x => x.Create).ShouldBe(true);
        }
    }

    [Test]
    public void ShouldGetSingleResourceByClaimSetIdAndResourceId()
    {
        var testApplication = new Application
        {
            ApplicationName = "TestApplicationName"
        };

        Save(testApplication);
        var testClaimSets = SetupApplicationWithClaimSets(testApplication).ToList();
        var rcIds = UniqueNameList("ParentRc", 1);
        var testResourceClaims = SetupParentResourceClaims(testClaimSets, testApplication, rcIds);

        foreach (var testClaimSet in testClaimSets)
        {
            var rcName = $"{rcIds.First()}{testClaimSet.ClaimSetName}";
            var testResourceClaim =
                testResourceClaims.Single(x => x.ClaimSet.ClaimSetId == testClaimSet.ClaimSetId && x.ResourceClaim.ResourceName == rcName).ResourceClaim;
            var result = SingleResourceClaimForClaimSet(testClaimSet.ClaimSetId, testResourceClaim.ResourceClaimId);

            result.Name.ShouldBe(testResourceClaim.ResourceName);
            result.Id.ShouldBe(testResourceClaim.ResourceClaimId);
            result.Create.ShouldBe(true);
            result.Read.ShouldBe(false);
            result.Update.ShouldBe(false);
            result.Delete.ShouldBe(false);
            result.ReadChanges.ShouldBe(false);
        }
    }

    [Test]
    public void ShouldGetParentResourcesWithChildrenByClaimSetId()
    {
        var testApplication = new Application
        {
            ApplicationName = "TestApplicationName"
        };

        Save(testApplication);
        var testClaimSets = SetupApplicationWithClaimSets(testApplication);
        var testResourceClaims = SetupParentResourceClaimsWithChildren(testClaimSets, testApplication);

        using var securityContext = TestContext;
        foreach (var testClaimSet in testClaimSets)
        {
            var results = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId).ToArray();

            var testParentResourceClaimsForId =
                testResourceClaims.Where(x => x.ClaimSet.ClaimSetId == testClaimSet.ClaimSetId && x.ResourceClaim.ParentResourceClaim == null).Select(x => x.ResourceClaim).ToArray();

            results.Length.ShouldBe(testParentResourceClaimsForId.Length);
            results.Select(x => x.Name).ShouldBe(testParentResourceClaimsForId.Select(x => x.ResourceName), true);
            results.Select(x => x.Id).ShouldBe(testParentResourceClaimsForId.Select(x => x.ResourceClaimId), true);
            results.All(x => x.Create).ShouldBe(true);

            foreach (var testParentResourceClaim in testParentResourceClaimsForId)
            {
                var testChildren = securityContext.ResourceClaims.Where(x =>
                    x.ParentResourceClaimId == testParentResourceClaim.ResourceClaimId).ToList();
                var parentResult = results.First(x => x.Id == testParentResourceClaim.ResourceClaimId);
                parentResult.Children.Select(x => x.Name).ShouldBe(testChildren.Select(x => x.ResourceName), true);
                parentResult.Children.Select(x => x.Id).ShouldBe(testChildren.Select(x => x.ResourceClaimId), true);
                parentResult.Children.All(x => x.Create).ShouldBe(true);
            }

        }
    }

    [Test]
    public void ShouldGetDefaultAuthorizationStrategiesForParentResourcesByClaimSetId()
    {
        var testApplication = new Application
        {
            ApplicationName = "TestApplicationName"
        };
        Save(testApplication);

        var testClaimSet = new ClaimSet
        {
            ClaimSetName = "TestClaimSet",
            Application = testApplication
        };
        Save(testClaimSet);

        var appAuthorizationStrategies = SetupApplicationAuthorizationStrategies(testApplication).ToList();
        var testResourceClaims = SetupParentResourceClaims([testClaimSet], testApplication, UniqueNameList("ParentRc", 3));
        var testAuthStrategies = SetupResourcesWithDefaultAuthorizationStrategies(appAuthorizationStrategies, [.. testResourceClaims]);

        var results = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId).ToArray();
        results.Select(x => x.DefaultAuthStrategiesForCRUD[0].AuthorizationStrategies[0].AuthStrategyName).ShouldBe(testAuthStrategies.Select(x => x.AuthorizationStrategies.Single()
                        .AuthorizationStrategy.AuthorizationStrategyName), true);

    }

    [Test]
    public void ShouldGetDefaultAuthorizationStrategiesForSingleResourcesByClaimSetIdAndResourceId()
    {
        var testApplication = new Application
        {
            ApplicationName = "TestApplicationName"
        };
        Save(testApplication);

        var testClaimSet = new ClaimSet
        {
            ClaimSetName = "TestClaimSet",
            Application = testApplication
        };
        Save(testClaimSet);

        var appAuthorizationStrategies = SetupApplicationAuthorizationStrategies(testApplication).ToList();
        var rcIds = UniqueNameList("Parent", 1);
        var testResourceClaims = SetupParentResourceClaims([testClaimSet], testApplication, rcIds);
        var testAuthStrategies = SetupResourcesWithDefaultAuthorizationStrategies(appAuthorizationStrategies, [.. testResourceClaims]);

        var rcName = $"{rcIds.First()}{testClaimSet.ClaimSetName}";
        var testResourceClaim =
            testResourceClaims.Single(x => x.ClaimSet.ClaimSetId == testClaimSet.ClaimSetId && x.ResourceClaim.ResourceName == rcName).ResourceClaim;
        var testAuthStrategy = testAuthStrategies.Single(x =>
            x.ResourceClaim.ResourceClaimId == testResourceClaim.ResourceClaimId && x.Action.ActionName == ActionName.Create.Value)
        .AuthorizationStrategies.Single().AuthorizationStrategy;

        var result = SingleResourceClaimForClaimSet(testClaimSet.ClaimSetId, testResourceClaim.ResourceClaimId);

        result.Name.ShouldBe(testResourceClaim.ResourceName);
        result.Id.ShouldBe(testResourceClaim.ResourceClaimId);
        result.Create.ShouldBe(true);
        result.Read.ShouldBe(false);
        result.Update.ShouldBe(false);
        result.Delete.ShouldBe(false);
        result.ReadChanges.ShouldBe(false);
        result.DefaultAuthStrategiesForCRUD[0].AuthorizationStrategies[0].AuthStrategyName.ShouldBe(testAuthStrategy.DisplayName);

    }

    [Test]
    public void ShouldGetDefaultAuthorizationStrategiesForParentResourcesWithChildrenByClaimSetId()
    {
        var testApplication = new Application
        {
            ApplicationName = "TestApplicationName"
        };
        Save(testApplication);

        var testClaimSet = new ClaimSet
        {
            ClaimSetName = "TestClaimSet",
            Application = testApplication
        };
        Save(testClaimSet);

        var appAuthorizationStrategies = SetupApplicationAuthorizationStrategies(testApplication).ToList();

        var testResourceClaims = SetupParentResourceClaimsWithChildren([testClaimSet], testApplication);
        var testAuthStrategies = SetupResourcesWithDefaultAuthorizationStrategies(appAuthorizationStrategies, [.. testResourceClaims]);

        var results = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId).ToArray();

        var testParentResourceClaimsForId =
            testResourceClaims
                .Where(x => x.ClaimSet.ClaimSetId == testClaimSet.ClaimSetId &&
                            x.ResourceClaim.ParentResourceClaim == null).Select(x => x.ResourceClaim).ToArray();

        var testAuthStrategiesForParents =
            testAuthStrategies.Where(x => x.ResourceClaim.ParentResourceClaim == null);

        results.Select(x => x.DefaultAuthStrategiesForCRUD[0].AuthorizationStrategies[0].AuthStrategyName).ShouldBe(testAuthStrategiesForParents.Select(x =>
        x.AuthorizationStrategies.Single().AuthorizationStrategy.AuthorizationStrategyName), true);

        foreach (var testParentResourceClaim in testParentResourceClaimsForId)
        {
            var parentResult = results.First(x => x.Id == testParentResourceClaim.ResourceClaimId);
            var testAuthStrategiesForChildren =
                testAuthStrategies.Where(x =>
                    x.ResourceClaim.ParentResourceClaimId == testParentResourceClaim.ResourceClaimId);
            parentResult.Children.Select(x => x.DefaultAuthStrategiesForCRUD[0].AuthorizationStrategies[0].AuthStrategyName).ShouldBe(testAuthStrategiesForChildren.Select(x => x.AuthorizationStrategies.Single()
            .AuthorizationStrategy.AuthorizationStrategyName), true);
        }
    }

    private IReadOnlyCollection<ClaimSet> SetupApplicationWithClaimSets(Application testApplication, int claimSetCount = 5)
    {
        var testClaimSetNames = Enumerable.Range(1, claimSetCount)
            .Select((x, index) => $"TestClaimSetName{index:N}")
            .ToArray();

        var testClaimSets = testClaimSetNames
            .Select(x => new ClaimSet
            {
                ClaimSetName = x,
                Application = testApplication
            })
            .ToArray();

        Save(testClaimSets.Cast<object>().ToArray());

        return testClaimSets;
    }

    private IReadOnlyCollection<ClaimSetResourceClaimAction> SetupParentResourceClaims(IEnumerable<ClaimSet> testClaimSets, Application testApplication, IList<string> resouceClaimsIds)
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
                    DisplayName = rcName,
                    ResourceName = rcName,
                    Application = testApplication
                };
                var action = new Action
                {
                    ActionName = ActionName.Create.Value,
                    ActionUri = "create"
                };
                var claimSetResourceClaim = new ClaimSetResourceClaimAction
                {
                    ResourceClaim = resourceClaim,
                    Action = action,
                    ClaimSet = claimSet
                };
                claimSetResourceClaims.Add(claimSetResourceClaim);
            }
        }

        Save(claimSetResourceClaims.Cast<object>().ToArray());

        return claimSetResourceClaims;
    }

    private IReadOnlyCollection<ClaimSetResourceClaimAction> SetupParentResourceClaimsWithChildren(IEnumerable<ClaimSet> testClaimSets, Application testApplication, int resourceClaimCount = 5, int childResourceClaimCount = 1)
    {
        var parentResourceClaims = new List<ResourceClaim>();
        var childResourceClaims = new List<ResourceClaim>();
        foreach (var parentIndex in Enumerable.Range(1, resourceClaimCount))
        {
            var resourceClaim = new ResourceClaim
            {
                ClaimName = $"TestParentResourceClaim{parentIndex:N}",
                DisplayName = $"TestParentResourceClaim{parentIndex:N}",
                ResourceName = $"TestParentResourceClaim{parentIndex:N}",
                Application = testApplication
            };
            parentResourceClaims.Add(resourceClaim);

            childResourceClaims.AddRange(Enumerable.Range(1, childResourceClaimCount)
                .Select(childIndex => new ResourceClaim
                {
                    ClaimName = $"TestChildResourceClaim{resourceClaim.ClaimName}",
                    DisplayName = $"TestChildResourceClaim{resourceClaim.ClaimName}",
                    ResourceName = $"TestChildResourceClaim{resourceClaim.ClaimName}",
                    Application = testApplication,
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

        claimSetResourceClaims = [];
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
