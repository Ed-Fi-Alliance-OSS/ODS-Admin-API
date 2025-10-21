// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.V1.Security.DataAccess.Models;
using NUnit.Framework;
using Shouldly;

using Application = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.Application;
using ResourceClaim = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.ResourceClaim;

namespace EdFi.Ods.AdminApi.V1.DBTests.Database.QueryTests;

[TestFixture]
public class GetResourceClaimsAsFlatListQueryTests : SecurityDataTestBase
{
    [Test]
    public void ShouldGetResourceClaimsAsFlatList()
    {
        var testApplication = new Application
        {
            ApplicationName = "TestApplicationName"
        };

        Save(testApplication);

        var testResourceClaims = SetupResourceClaims(testApplication);

        V1.Infrastructure.Services.ClaimSetEditor.ResourceClaim[] results = null;
        using var securityContext = TestContext;
        var query = new GetResourceClaimsAsFlatListQuery(securityContext);
        results = [.. query.Execute()];
        results.Length.ShouldBe(testResourceClaims.Count);
        results.Select(x => x.Name).ShouldBe(testResourceClaims.Select(x => x.ResourceName), true);
        results.Select(x => x.Id).ShouldBe(testResourceClaims.Select(x => x.ResourceClaimId), true);
        results.All(x => x.Create == false).ShouldBe(true);
        results.All(x => x.Delete == false).ShouldBe(true);
        results.All(x => x.Update == false).ShouldBe(true);
        results.All(x => x.Read == false).ShouldBe(true);
        results.All(x => x.ParentId.Equals(0)).ShouldBe(true);
        results.All(x => x.ParentName == null).ShouldBe(true);
        results.All(x => x.Children.Count == 0).ShouldBe(true);
    }

    [Test]
    public void ShouldGetAlphabeticallySortedFlatListForResourceClaims()
    {
        var testApplication = new Application
        {
            ApplicationName = "TestApplicationName"
        };

        Save(testApplication);

        var testClaimSet = new ClaimSet
        { ClaimSetName = "TestClaimSet_test", Application = testApplication };
        Save(testClaimSet);
        var testResourceClaims = SetupParentResourceClaimsWithChildren(testClaimSet, testApplication, UniqueNameList("ParentRc", 3), UniqueNameList("ChildRc", 1)).ToList();
        var parentResourceNames = testResourceClaims.Where(x => x.ResourceClaim?.ParentResourceClaim == null)
            .OrderBy(x => x.ResourceClaim.ResourceName).Select(x => x.ResourceClaim?.ResourceName).ToList();
        var childResourceNames = testResourceClaims.Where(x => x.ResourceClaim?.ParentResourceClaim != null)
            .OrderBy(x => x.ResourceClaim?.ResourceName).Select(x => x.ResourceClaim?.ResourceName).ToList();

        List<V1.Infrastructure.Services.ClaimSetEditor.ResourceClaim> results = null;
        using var securityContext = TestContext;
        var query = new GetResourceClaimsAsFlatListQuery(securityContext);
        results = [.. query.Execute()];
        results.Count.ShouldBe(testResourceClaims.Count);
        results.Where(x => x.ParentId == 0).Select(x => x.Name).ToList().ShouldBe(parentResourceNames);
        results.Where(x => x.ParentId != 0).Select(x => x.Name).ToList().ShouldBe(childResourceNames);
    }

    private IReadOnlyCollection<ResourceClaim> SetupResourceClaims(Application testApplication, int resourceClaimCount = 5)
    {
        var resourceClaims = new List<ResourceClaim>();
        foreach (var index in Enumerable.Range(1, resourceClaimCount))
        {
            var resourceClaim = new ResourceClaim
            {
                ClaimName = $"TestResourceClaim{index:N}",
                DisplayName = $"TestResourceClaim{index:N}",
                ResourceName = $"TestResourceClaim{index:N}",
                Application = testApplication
            };
            resourceClaims.Add(resourceClaim);
        }

        Save(resourceClaims.Cast<object>().ToArray());

        return resourceClaims;
    }

    //private IReadOnlyCollection<ResourceClaim> SetupParentResourceClaimsWithChildren(Application testApplication, int resourceClaimCount = 5, int childResourceClaimCount = 3)
    //{
    //    var parentResourceClaims = Enumerable.Range(1, resourceClaimCount).Select(parentIndex => new ResourceClaim
    //    {
    //        ClaimName = $"TestParentResourceClaim{parentIndex}",
    //        DisplayName = $"TestParentResourceClaim{parentIndex}",
    //        ResourceName = $"TestParentResourceClaim{parentIndex}",
    //        Application = testApplication
    //    }).ToList();

    //    var childResourceClaims = parentResourceClaims.SelectMany(x => Enumerable.Range(1, childResourceClaimCount)
    //        .Select(childIndex => new ResourceClaim
    //        {
    //            ClaimName = $"TestChildResourceClaim{childIndex}",
    //            DisplayName = $"TestChildResourceClaim{childIndex}",
    //            ResourceName = $"TestChildResourceClaim{childIndex}",
    //            Application = testApplication,
    //            ParentResourceClaim = x
    //        })).ToList();

    //    Save(childResourceClaims.Cast<object>().ToArray());
    //    parentResourceClaims.AddRange(childResourceClaims);
    //    return parentResourceClaims;
    //}
}
