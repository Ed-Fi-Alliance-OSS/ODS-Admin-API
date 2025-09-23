// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FluentValidation;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.DBTests.ClaimSetEditorTests;

[TestFixture]
public class OverrideDefaultAuthorizationStrategyCommandTests : SecurityDataTestBase
{
    [Test]
    public void ShouldOverrideAuthorizationStrategiesForParentResourcesOnClaimSet()
    {
        InitializeData(out var testClaimSet, out var appAuthorizationStrategies, out var testResource1ToEdit, out var testResource2ToNotEdit);
        var overrides = new List<AuthorizationStrategy>();
        if (appAuthorizationStrategies != null)
        {
            foreach (var appAuthorizationStrategy in appAuthorizationStrategies)
            {
                overrides.Add(new AuthorizationStrategy
                {
                    AuthStrategyId = appAuthorizationStrategy.AuthorizationStrategyId,
                    AuthStrategyName = appAuthorizationStrategy.AuthorizationStrategyName
                });
            }
        }

        var overrideModel = new OverrideAuthorizationStrategyModel
        {
            ResourceClaimId = testResource1ToEdit.ResourceClaimId,
            ClaimSetId = testClaimSet.ClaimSetId,
            ClaimSetResourceClaimActionAuthStrategyOverrides = new List<ClaimSetResourceClaimActionAuthStrategies> {
                new ClaimSetResourceClaimActionAuthStrategies
                {
                    ActionId = 1,
                    ActionName= "Create",
                    AuthorizationStrategies = overrides
                }
            }
        };

        List<ResourceClaim> resourceClaimsForClaimSet = null;

        using var securityContext = TestContext;
        var command = new OverrideDefaultAuthorizationStrategyCommand(securityContext);
        command.Execute(overrideModel);
        var getResourcesByClaimSetIdQuery = new GetResourcesByClaimSetIdQuery(securityContext, SecurityDataTestBase.Mapper());
        resourceClaimsForClaimSet = getResourcesByClaimSetIdQuery.AllResources(testClaimSet.ClaimSetId).ToList();

        var resultResourceClaim1 =
            resourceClaimsForClaimSet.Single(x => x.Id == overrideModel.ResourceClaimId);

        resultResourceClaim1.AuthorizationStrategyOverridesForCRUD.Count.ShouldBe(1);
        resultResourceClaim1.AuthorizationStrategyOverridesForCRUD[0].ActionName.ShouldBe("Create");
        resultResourceClaim1.AuthorizationStrategyOverridesForCRUD[0].AuthorizationStrategies.First().AuthStrategyName.ShouldBe("TestAuthStrategy1");

        var resultResourceClaim2 =
            resourceClaimsForClaimSet.Single(x => x.Id == testResource2ToNotEdit.ResourceClaimId);

        resultResourceClaim2.AuthorizationStrategyOverridesForCRUD.ShouldBeEmpty();
    }

    [Test]
    public void ShouldOverrideAuthorizationStrategiesForSpecificResourcesOnClaimSet()
    {
        InitializeData(out var testClaimSet, out var appAuthorizationStrategies, out var testResource1ToEdit, out var testResource2ToNotEdit);

        var overrides = new List<int>();
        if (appAuthorizationStrategies != null)
        {
            foreach (var appAuthorizationStrategy in appAuthorizationStrategies)
            {
                overrides.Add(appAuthorizationStrategy.AuthorizationStrategyId);
            }
        }
        var overrideModel = new OverrideAuthStrategyOnClaimSetModel
        {
            ResourceClaimId = testResource1ToEdit.ResourceClaimId,
            ClaimSetId = testClaimSet.ClaimSetId,
            ActionName = "Create",
            AuthStrategyIds = overrides
        };

        List<ResourceClaim> resourceClaimsForClaimSet = null;

        using var securityContext = TestContext;
        var command = new OverrideDefaultAuthorizationStrategyCommand(securityContext);
        command.ExecuteOnSpecificAction(overrideModel);
        var getResourcesByClaimSetIdQuery = new GetResourcesByClaimSetIdQuery(securityContext, SecurityDataTestBase.Mapper());
        resourceClaimsForClaimSet = getResourcesByClaimSetIdQuery.AllResources(testClaimSet.ClaimSetId).ToList();

        var resultResourceClaim1 =
            resourceClaimsForClaimSet.Single(x => x.Id == overrideModel.ResourceClaimId);

        resultResourceClaim1.AuthorizationStrategyOverridesForCRUD.Count.ShouldBe(1);
        resultResourceClaim1.AuthorizationStrategyOverridesForCRUD[0].ActionName.ShouldBe("Create");
        resultResourceClaim1.AuthorizationStrategyOverridesForCRUD[0].AuthorizationStrategies.Count().ShouldBe(4);

        var resultResourceClaim2 =
            resourceClaimsForClaimSet.Single(x => x.Id == testResource2ToNotEdit.ResourceClaimId);

        resultResourceClaim2.AuthorizationStrategyOverridesForCRUD.ShouldBeEmpty();
    }

    [Test]
    public void ShouldOverrideAuthorizationStrategiesForSpecificResourcesOnClaimSetDefaultAuth()
    {
        InitializeData(out var testClaimSet, out var appAuthorizationStrategies, out var testResource1ToEdit, out var testResource2ToNotEdit);

        var overrides = appAuthorizationStrategies.Select(a => a.AuthorizationStrategyId).ToList();

        var resourceClaimActionParent = TestContext.ResourceClaimActions.First(rca => rca.ResourceClaimId == testResource1ToEdit.ResourceClaimId && rca.Action.ActionName == "Create");

        var resourceClaimActionAuthStrategiesParent = TestContext.ResourceClaimActionAuthorizationStrategies.First(rcaa => rcaa.ResourceClaimActionId == resourceClaimActionParent.ResourceClaimActionId);

        overrides.Add(resourceClaimActionAuthStrategiesParent.AuthorizationStrategyId);

        var overrideModel = new OverrideAuthStrategyOnClaimSetModel
        {
            ResourceClaimId = testResource1ToEdit.ResourceClaimId,
            ClaimSetId = testClaimSet.ClaimSetId,
            ActionName = "Create",
            AuthStrategyIds = overrides
        };

        List<ResourceClaim> resourceClaimsForClaimSet = null;

        using var securityContext = TestContext;
        var command = new OverrideDefaultAuthorizationStrategyCommand(securityContext);
        command.ExecuteOnSpecificAction(overrideModel);
        var getResourcesByClaimSetIdQuery = new GetResourcesByClaimSetIdQuery(securityContext, SecurityDataTestBase.Mapper());
        resourceClaimsForClaimSet = getResourcesByClaimSetIdQuery.AllResources(testClaimSet.ClaimSetId).ToList();

        var resultResourceClaim1 =
            resourceClaimsForClaimSet.Single(x => x.Id == overrideModel.ResourceClaimId);

        resultResourceClaim1.AuthorizationStrategyOverridesForCRUD.Count.ShouldBe(1);
        resultResourceClaim1.AuthorizationStrategyOverridesForCRUD[0].ActionName.ShouldBe("Create");

        var resourceClaimActionAuthorizationStrategies = new GetResourceClaimActionAuthorizationStrategiesQuery(securityContext, null).Execute(new Common.Infrastructure.CommonQueryParams(), null);
        var authStrategyName = resourceClaimActionAuthorizationStrategies
            .FirstOrDefault(p => p.ResourceClaimId == overrideModel.ResourceClaimId).AuthorizationStrategiesForActions
            .FirstOrDefault(s => s.ActionName.Equals(overrideModel.ActionName, StringComparison.CurrentCultureIgnoreCase)).AuthorizationStrategies.FirstOrDefault().AuthStrategyName;

        resultResourceClaim1.AuthorizationStrategyOverridesForCRUD[0].AuthorizationStrategies.Any(x => x.AuthStrategyName.Equals(authStrategyName)).ShouldBeFalse();

        var resultResourceClaim2 =
            resourceClaimsForClaimSet.Single(x => x.Id == testResource2ToNotEdit.ResourceClaimId);

        resultResourceClaim2.AuthorizationStrategyOverridesForCRUD.ShouldBeEmpty();
    }

    [Test]
    public void ShouldThrowErrorWhenActionIsNotEnabledForAResource()
    {
        InitializeData(out var testClaimSet, out var appAuthorizationStrategies, out var testResource1ToEdit, out var testResource2ToNotEdit);
        var action = "Update";
        var overrides = new List<int>();
        if (appAuthorizationStrategies != null)
        {
            foreach (var appAuthorizationStrategy in appAuthorizationStrategies)
            {
                overrides.Add(appAuthorizationStrategy.AuthorizationStrategyId);
            }
        }
        var overrideModel = new OverrideAuthStrategyOnClaimSetModel
        {
            ResourceClaimId = testResource1ToEdit.ResourceClaimId,
            ClaimSetId = testClaimSet.ClaimSetId,
            ActionName = action,
            AuthStrategyIds = overrides
        };

        var badRequestException = Assert.Throws<ValidationException>(() =>
        {
            using var securityContext = TestContext;
            var command = new OverrideDefaultAuthorizationStrategyCommand(securityContext);
            command.ExecuteOnSpecificAction(overrideModel);
        });
        badRequestException.ShouldNotBeNull();
        badRequestException.Errors.First().ErrorMessage.ShouldContain($"{action} action is not enabled for the resource claim with");

    }

    private void InitializeData(out ClaimSet testClaimSet, out List<Security.DataAccess.Models.AuthorizationStrategy> appAuthorizationStrategies, out Security.DataAccess.Models.ResourceClaim testResource1ToEdit, out Security.DataAccess.Models.ResourceClaim testResource2ToNotEdit)
    {
        testClaimSet = new ClaimSet
        {
            ClaimSetName = "TestClaimSet",
        };
        Save(testClaimSet);

        appAuthorizationStrategies = SetupApplicationAuthorizationStrategies().ToList();
        var parentRcNames = UniqueNameList("ParentRc", 2);

        var testResourceClaims = SetupClaimSetResourceClaimActions(
            testClaimSet, parentRcNames, UniqueNameList("Child", 1));

        SetupResourcesWithDefaultAuthorizationStrategies(
            appAuthorizationStrategies, testResourceClaims.ToList());

        testResource1ToEdit = testResourceClaims.Select(x => x.ResourceClaim)
            .Single(x => x.ResourceName == parentRcNames.First());
        testResource2ToNotEdit = testResourceClaims.Select(x => x.ResourceClaim)
            .Single(x => x.ResourceName == parentRcNames.Last());
    }
}
