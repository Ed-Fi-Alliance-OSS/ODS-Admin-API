// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using EdFi.Ods.AdminApi.V1.Infrastructure.AutoMapper;
using EdFi.Ods.AdminApi.V1.Security.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Security.DataAccess.Models;
using NUnit.Framework;
using Action = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.Action;
using ActionName = EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor.Action;
using ClaimSetEditorTypes = EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;

namespace EdFi.Ods.AdminApi.V1.DBTests;

[TestFixture]
public abstract class SecurityDataTestBase : PlatformSecurityContextTestBase
{
    protected override string ConnectionString => Testing.SecurityConnectionString;

    protected override SqlServerSecurityContext CreateDbContext()
    {
        return new SqlServerSecurityContext(Testing.GetDbContextOptions(ConnectionString));
    }

    // This bool controls whether or not to run SecurityContext initialization
    // method. Setting this flag to true will cause seed data to be
    // inserted into the security database on fixture setup.
    protected bool SeedSecurityContextOnFixtureSetup { get; set; } = false;

    protected override void AdditionalFixtureSetup()
    {
        if (SeedSecurityContextOnFixtureSetup)
        {
            TestContext.Database.EnsureCreated();
        }
    }

    protected void LoadSeedData()
    {
        var odsApplication = GetOrCreateApplication("Ed-Fi ODS API");

        var readAction = GetOrCreateAction("Read");
        GetOrCreateAction("Create");
        GetOrCreateAction("Update");
        GetOrCreateAction("Delete");


        GetOrCreateAuthorizationStrategy(odsApplication, "Namespace Based", "NamespaceBased");

        var noFurtherStrategy = GetOrCreateAuthorizationStrategy(odsApplication, "No Further Authorization Required",
            "NoFurtherAuthorizationRequired");


        var educationStandardsResourceClaim = GetOrCreateResourceClaim("educationStandards", odsApplication);
        GetOrCreateResourceClaim("types", odsApplication);
        GetOrCreateResourceClaim("managedDescriptors", odsApplication);
        GetOrCreateResourceClaim("systemDescriptors", odsApplication);
        GetOrCreateResourceClaim("educationOrganizations", odsApplication);


        GetOrCreateResourceClaimAuthorizationMetadata(readAction, noFurtherStrategy,
            educationStandardsResourceClaim);

        TestContext.SaveChanges();

        Application GetOrCreateApplication(string applicationName)
        {
            var application =
                TestContext.Applications.FirstOrDefault(a => a.ApplicationName == applicationName);
            if (application == null)
            {
                application = new Application
                {
                    ApplicationName = "Ed-Fi ODS API"
                };
                TestContext.Applications.Add(application);
            }
            return application;
        }

        Action GetOrCreateAction(string actionName)
        {
            var action = TestContext.Actions.FirstOrDefault(a => a.ActionName == actionName);

            if (action == null)
            {
                action = new Action
                {
                    ActionName = actionName,
                    ActionUri = $"http://ed-fi.org/odsapi/actions/{actionName}"
                };
                TestContext.Actions.Add(action);
            }
            return action;
        }

        AuthorizationStrategy GetOrCreateAuthorizationStrategy(Application application, string displayName,
            string authorizationStrategyName)
        {
            var authorizationStrategy = TestContext.AuthorizationStrategies.FirstOrDefault(
                a =>
                    a.Application.ApplicationId == application.ApplicationId &&
                    a.DisplayName == displayName &&
                    a.AuthorizationStrategyName == authorizationStrategyName);

            if (authorizationStrategy == null)
            {
                authorizationStrategy =
                    new AuthorizationStrategy
                    {
                        DisplayName = displayName,
                        AuthorizationStrategyName = authorizationStrategyName,
                        Application = application
                    };
                TestContext.AuthorizationStrategies.Add(authorizationStrategy);
            }

            return authorizationStrategy;
        }

        ResourceClaim GetOrCreateResourceClaim(string resourceName, Application application)
        {
            var resourceClaim =
                TestContext.ResourceClaims.FirstOrDefault(
                    r =>
                        r.ResourceName == resourceName &&
                        r.Application.ApplicationId == application.ApplicationId);

            if (resourceClaim == null)
            {
                resourceClaim = new ResourceClaim
                {
                    Application = application,
                    DisplayName = resourceName,
                    ResourceName = resourceName,
                    ClaimName = $"http://ed-fi.org/ods/identity/claims/domains/{resourceName}",
                    ParentResourceClaim = null
                };
                TestContext.ResourceClaims.Add(resourceClaim);
            }
            return resourceClaim;
        }

        void GetOrCreateResourceClaimAuthorizationMetadata(Action action,
                   AuthorizationStrategy authorizationStrategy,
                   ResourceClaim resourceClaim)
        {
            var resourceClaimAuthorizationMetadata = TestContext.ResourceClaimActions.FirstOrDefault(rcm =>
                    rcm.Action.ActionId == action.ActionId && rcm.AuthorizationStrategies.FirstOrDefault()
                    .AuthorizationStrategyId == authorizationStrategy.AuthorizationStrategyId &&
                    rcm.ResourceClaim.ResourceClaimId == resourceClaim.ResourceClaimId);

            if (resourceClaimAuthorizationMetadata == null)
            {
                TestContext.ResourceClaimActions.Add(new ResourceClaimAction
                {
                    Action = action,
                    AuthorizationStrategies = authorizationStrategy != null ?
                    [
                               new() {
                                   AuthorizationStrategy = authorizationStrategy,
                                   ResourceClaimAction = resourceClaimAuthorizationMetadata
                               }
                    ] : null,
                    ResourceClaim = resourceClaim,
                    ValidationRuleSetName = null
                });
            }
        }
    }

    protected IReadOnlyCollection<ResourceClaim> SetupResourceClaims(Application testApplication, IList<string> parentRcNames, IList<string> childRcNames)
    {
        var parentResourceClaims = new List<ResourceClaim>();
        var childResourceClaims = new List<ResourceClaim>();
        var actions = new List<Action>();
        foreach (var parentName in parentRcNames)
        {
            var resourceClaim = new ResourceClaim
            {
                ClaimName = parentName,
                DisplayName = parentName,
                ResourceName = parentName,
                Application = testApplication
            };
            parentResourceClaims.Add(resourceClaim);

            childResourceClaims.AddRange(childRcNames
                .Select(childName =>
                {
                    var childRcName = $"{childName}-{parentName}";
                    return new ResourceClaim
                    {
                        ClaimName = childRcName,
                        DisplayName = childRcName,
                        ResourceName = childRcName,
                        Application = testApplication,
                        ParentResourceClaim = resourceClaim,
                        ParentResourceClaimId = resourceClaim.ResourceClaimId
                    };
                }));
        }

        foreach (var action in ActionName.GetAll())
        {
            var actionObject = new Action
            {
                ActionName = action.Value,
                ActionUri = action.Value
            };
            actions.Add(actionObject);
        }

        Save(parentResourceClaims.Cast<object>().ToArray());
        Save(childResourceClaims.Cast<object>().ToArray());
        Save(actions.Cast<object>().ToArray());

        return parentResourceClaims;
    }

    public static IList<string> UniqueNameList(string prefix, int resourceClaimCount = 5)
    {
        var random = new Random();
        var parentResourceClaims = Enumerable.Range(1, resourceClaimCount).Select(index =>
        {
            return $"{prefix}{random.Next()}";
        }).ToList();

        return parentResourceClaims;
    }

    protected IReadOnlyCollection<ClaimSetResourceClaimAction> SetupParentResourceClaimsWithChildren(ClaimSet testClaimSet, Application testApplication, IList<string> parentRcNames, IList<string> childRcNames)
    {
        var actions = ActionName.GetAll().Select(action => new Action { ActionName = action.Value, ActionUri = action.Value }).ToList();
        Save(actions.Cast<object>().ToArray());

        var parentResourceClaims = parentRcNames.Select(parentRcName =>
        {
            return new ResourceClaim
            {
                ClaimName = parentRcName,
                DisplayName = parentRcName,
                ResourceName = parentRcName,
                Application = testApplication
            };
        }).ToList();

        var childResourceClaims = parentResourceClaims.SelectMany(x => childRcNames
           .Select(childRcName =>
           {
               var childName = $"{childRcName}-{x.ClaimName}";
               return new ResourceClaim
               {
                   ClaimName = childName,
                   DisplayName = childName,
                   ResourceName = childName,
                   Application = testApplication,
                   ParentResourceClaim = x
               };
           })).ToList();

        Save(childResourceClaims.Cast<object>().ToArray());

        var claimSetResourceClaims = Enumerable.Range(1, parentRcNames.Count)
            .Select(index => parentResourceClaims[index - 1]).Select(parentResource => new ClaimSetResourceClaimAction
            {
                ResourceClaim = parentResource,
                Action = actions.Single(x => x.ActionName == ActionName.Create.Value),
                ClaimSet = testClaimSet
            }).ToList();

        var childResources = parentResourceClaims.SelectMany(x => childResourceClaims
            .Where(child => child.ParentResourceClaimId == x.ResourceClaimId)
            .Select(child => new ClaimSetResourceClaimAction
            {
                ResourceClaim = child,
                Action = actions.Single(a => a.ActionName == ActionName.Create.Value),
                ClaimSet = testClaimSet
            }).ToList()).ToList();

        claimSetResourceClaims.AddRange(childResources);

        Save(claimSetResourceClaims.Cast<object>().ToArray());

        return claimSetResourceClaims;
    }

    protected IReadOnlyCollection<AuthorizationStrategy> SetupApplicationAuthorizationStrategies(Application testApplication, int authStrategyCount = 5)
    {
        var testAuthStrategies = Enumerable.Range(1, authStrategyCount)
            .Select(index => $"TestAuthStrategy{index}")
            .ToArray();

        var authStrategies = testAuthStrategies
            .Select(x => new AuthorizationStrategy
            {
                AuthorizationStrategyName = x,
                DisplayName = x,
                Application = testApplication
            })
            .ToArray();

        Save(authStrategies.Cast<object>().ToArray());

        return authStrategies;
    }

    protected IReadOnlyCollection<ResourceClaimAction> SetupResourcesWithDefaultAuthorizationStrategies(List<AuthorizationStrategy> testAuthorizationStrategies, List<ClaimSetResourceClaimAction> claimSetResourceClaims)
    {
        var resourceClaimWithDefaultAuthStrategies = new List<ResourceClaimAction>();
        var random = new Random();
        foreach (var resourceClaim in claimSetResourceClaims)
        {
            var testAuthorizationStrategy = testAuthorizationStrategies[random.Next(testAuthorizationStrategies.Count)];

            var resourceClaimWithDefaultAuthStrategy = new ResourceClaimAction
            {
                ResourceClaim = resourceClaim.ResourceClaim,
                Action = resourceClaim.Action,
            };

            var rcActionAuthorizationStrategies = testAuthorizationStrategy != null ?
                   new List<ResourceClaimActionAuthorizationStrategies> {
                    new() { AuthorizationStrategy = testAuthorizationStrategy, ResourceClaimAction = resourceClaimWithDefaultAuthStrategy } } : null;

            resourceClaimWithDefaultAuthStrategy.AuthorizationStrategies = rcActionAuthorizationStrategies;
            resourceClaimWithDefaultAuthStrategies.Add(resourceClaimWithDefaultAuthStrategy);
        }

        Save(resourceClaimWithDefaultAuthStrategies.Cast<object>().ToArray());

        return resourceClaimWithDefaultAuthStrategies;
    }

    protected static IMapper Mapper() => new MapperConfiguration(cfg => cfg.AddProfile<AdminApiMappingProfile>()).CreateMapper();

    protected List<ClaimSetEditorTypes.ResourceClaim> ResourceClaimsForClaimSet(int securityContextClaimSetId)
    {
        List<ClaimSetEditorTypes.ResourceClaim> list = null;
        using (var securityContext = CreateDbContext())
        {
            var getResourcesByClaimSetIdQuery = new ClaimSetEditorTypes.GetResourcesByClaimSetIdQuery(new ClaimSetEditorTypes.GetResourcesByClaimSetIdQueryService(securityContext, Mapper()));
            list = [.. getResourcesByClaimSetIdQuery.AllResources(securityContextClaimSetId)];
        }
        return list;
    }

    protected ClaimSetEditorTypes.ResourceClaim SingleResourceClaimForClaimSet(int securityContextClaimSetId, int resourceClaimId)
    {
        ClaimSetEditorTypes.ResourceClaim resourceClaim = null;
        using (var securityContext = CreateDbContext())
        {
            var getResourcesByClaimSetIdQuery = new ClaimSetEditorTypes.GetResourcesByClaimSetIdQuery(new ClaimSetEditorTypes.GetResourcesByClaimSetIdQueryService(securityContext, Mapper()));
            resourceClaim = getResourcesByClaimSetIdQuery.SingleResource(securityContextClaimSetId, resourceClaimId);
        }
        return resourceClaim;
    }
}
