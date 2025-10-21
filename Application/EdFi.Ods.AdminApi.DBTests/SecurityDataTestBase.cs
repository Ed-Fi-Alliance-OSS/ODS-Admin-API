// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using NUnit.Framework;
using Action = EdFi.Security.DataAccess.Models.Action;
using ActionName = EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor.Action;
using Application = EdFi.Admin.DataAccess.Models.Application;
using ClaimSetEditorTypes = EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;

namespace EdFi.Ods.AdminApi.DBTests;

[TestFixture]
public abstract class SecurityDataTestBase : PlatformSecurityContextTestBase
{
    protected override string ConnectionString => Testing.SecurityConnectionString;

    protected override SqlServerSecurityContext CreateDbContext()
    {
        return new SqlServerSecurityContext(Testing.GetDbContextOptions(ConnectionString));
    }
    public virtual string AdminTestingConnectionString => Testing.AdminConnectionString;

    public virtual SqlServerUsersContext AdminDbContext => new(Testing.GetDbContextOptions(AdminTestingConnectionString));

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
            var application = AdminDbContext.Applications.FirstOrDefault(a => a.ApplicationName == applicationName);

            if (application == null)
            {
                application = new Application
                {
                    ApplicationName = "Ed-Fi ODS API"
                };
                AdminDbContext.Applications.Add(application);
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
            var authorizationStrategy = TestContext.AuthorizationStrategies.FirstOrDefault(a =>
                                            a.DisplayName == displayName &&
                                            a.AuthorizationStrategyName == authorizationStrategyName);

            if (authorizationStrategy == null)
            {
                authorizationStrategy = new AuthorizationStrategy
                {
                    DisplayName = displayName,
                    AuthorizationStrategyName = authorizationStrategyName,
                };
                TestContext.AuthorizationStrategies.Add(authorizationStrategy);
            }

            return authorizationStrategy;
        }

        ResourceClaim GetOrCreateResourceClaim(string resourceName, Application application)
        {
            var resourceClaim =
                TestContext.ResourceClaims.FirstOrDefault(r =>
                    r.ResourceName == resourceName);
            if (resourceClaim == null)
            {
                resourceClaim = new ResourceClaim
                {
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
                    [ new ResourceClaimActionAuthorizationStrategies
                    { AuthorizationStrategy = authorizationStrategy} ] : null,
                    ResourceClaim = resourceClaim,
                    ValidationRuleSetName = null
                });
            }
        }
    }

    protected IReadOnlyCollection<ResourceClaim> SetupResourceClaims(IList<string> parentRcNames, IList<string> childRcNames)
    {
        var parentResourceClaims = new List<ResourceClaim>();
        var childResourceClaims = new List<ResourceClaim>();
        var actions = new List<Action>();
        foreach (var parentName in parentRcNames)
        {
            var resourceClaim = new ResourceClaim
            {
                ClaimName = parentName,
                ResourceName = parentName,
            };
            parentResourceClaims.Add(resourceClaim);

            childResourceClaims.AddRange(childRcNames
                .Select(childName =>
                {
                    var childRcName = $"{childName}-{parentName}";
                    return new ResourceClaim
                    {
                        ClaimName = childRcName,
                        ResourceName = childRcName,
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

    protected IReadOnlyCollection<ClaimSetResourceClaimAction> SetupClaimSetResourceClaimActions(
        ClaimSet testClaimSet,
        IList<string> parentRcNames,
        IList<string> childRcNames,
        IList<string> grandChildRcNames = null
        )
    {
        var actions = ActionName.GetAll().Select(action => new Action { ActionName = action.Value, ActionUri = action.Value }).ToList();
        Save(actions.Cast<object>().ToArray());

        var resourceClaims = SetupResourceClaimsWithChildren(parentRcNames, childRcNames, grandChildRcNames);

        var parentResourceClaims = resourceClaims.Where(rc => rc.ParentResourceClaim == null).ToList();
        var childResourceClaims = resourceClaims.Where(rc => rc.ParentResourceClaim != null && rc.ParentResourceClaim.ParentResourceClaim == null).ToList();
        var grandChildResourceClaims = resourceClaims.Where(rc => rc.ParentResourceClaim != null && rc.ParentResourceClaim.ParentResourceClaim != null).ToList();


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

        var grandChildResources = grandChildResourceClaims.Select(grandChild => new ClaimSetResourceClaimAction
        {
            ResourceClaim = grandChild,
            Action = actions.Single(a => a.ActionName == ActionName.Create.Value),
            ClaimSet = testClaimSet
        }).ToList();
        claimSetResourceClaims.AddRange(grandChildResources);

        Save(claimSetResourceClaims.Cast<object>().ToArray());

        return claimSetResourceClaims;
    }


    protected IReadOnlyCollection<ResourceClaim> SetupResourceClaimsWithChildren(
    IList<string> parentRcNames,
    IList<string> childRcNames,
    IList<string> grandChildRcNames = null
    )
    {
        var parentResourceClaims = parentRcNames.Select(parentRcName =>
        {
            return new ResourceClaim
            {
                ClaimName = parentRcName,
                ResourceName = parentRcName,
            };
        }).ToList();

        var childResourceClaims = parentResourceClaims.SelectMany(x => childRcNames
           .Select(childRcName =>
           {
               var childName = $"{childRcName}-{x.ClaimName}";
               return new ResourceClaim
               {
                   ClaimName = childName,
                   ResourceName = childName,
                   ParentResourceClaim = x
               };
           })).ToList();

        var grandChildResourceClaims = grandChildRcNames == null || !grandChildRcNames.Any() ? [] : childResourceClaims.SelectMany(child => grandChildRcNames.Select(grandChildName =>
            {
                var fullName = $"{grandChildName}-{child.ClaimName}";
                return new ResourceClaim
                {
                    ClaimName = fullName,
                    ResourceName = fullName,
                    ParentResourceClaim = child
                };
            })).ToList();

        var allResourceClaims = parentResourceClaims
        .Concat(childResourceClaims)
        .Concat(grandChildResourceClaims)
        .ToList();

        Save(allResourceClaims.Cast<object>().ToArray());
        return allResourceClaims;

    }


    protected IReadOnlyCollection<AuthorizationStrategy> SetupApplicationAuthorizationStrategies(int authStrategyCount = 5)
    {
        var testAuthStrategies = Enumerable.Range(1, authStrategyCount)
            .Select(index => $"TestAuthStrategy{index}")
            .ToArray();

        var authStrategies = testAuthStrategies
            .Select(x => new AuthorizationStrategy
            {
                AuthorizationStrategyName = x,
                DisplayName = x,
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

            var rcActionAuthorizationStrategies = testAuthorizationStrategy != null ?
                   new List<ResourceClaimActionAuthorizationStrategies> {
                    new() { AuthorizationStrategy = testAuthorizationStrategy } } : null;

            var resourceClaimWithDefaultAuthStrategy = new ResourceClaimAction
            {
                ResourceClaim = resourceClaim.ResourceClaim,
                Action = resourceClaim.Action,
                AuthorizationStrategies = rcActionAuthorizationStrategies
            };
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
            var getResourcesByClaimSetIdQuery = new ClaimSetEditorTypes.GetResourcesByClaimSetIdQuery(securityContext, Mapper());
            list = [.. getResourcesByClaimSetIdQuery.AllResources(securityContextClaimSetId)];
        }
        return list;
    }

    protected ClaimSetEditorTypes.ResourceClaim SingleResourceClaimForClaimSet(int securityContextClaimSetId, int resourceClaimId)
    {
        ClaimSetEditorTypes.ResourceClaim resourceClaim = null;
        using (var securityContext = CreateDbContext())
        {
            var getResourcesByClaimSetIdQuery = new ClaimSetEditorTypes.GetResourcesByClaimSetIdQuery(securityContext, Mapper());
            resourceClaim = getResourcesByClaimSetIdQuery.SingleResource(securityContextClaimSetId, resourceClaimId);
        }
        return resourceClaim;
    }
}
