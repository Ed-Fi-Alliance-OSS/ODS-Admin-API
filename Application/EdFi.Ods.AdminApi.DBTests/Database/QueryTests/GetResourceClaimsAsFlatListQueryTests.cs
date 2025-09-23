// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Security.DataAccess.Models;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.QueryTests;

[TestFixture]
public class GetResourceClaimsAsFlatListQueryTests : SecurityDataTestBase
{
    [Test]
    public void ShouldGetResourceClaimsAsFlatList()
    {
        var parentPrefix = "ParentRc";
        var childPrefix = "ChildRc";
        var grandChildPrefix = "GrandChildRc";
        var parentResourceNames = UniqueNameList(parentPrefix, 3);
        var childrenResourceNames = UniqueNameList(childPrefix, 2);
        var grandChildResourceNames = UniqueNameList(grandChildPrefix, 2);

        var testResourceClaims = SetupResourceClaimsWithChildren(
                parentResourceNames,
                childrenResourceNames,
                grandChildResourceNames);

        Infrastructure.ClaimSetEditor.ResourceClaim[] results = null;
        using var securityContext = TestContext;
        var query = new GetResourceClaimsAsFlatListQuery(securityContext);
        results = query.Execute().ToArray();
        results.Length.ShouldBe(testResourceClaims.Count);
        results.Select(x => x.Name).ShouldBe(testResourceClaims.Select(x => x.ResourceName), true);
        results.Select(x => x.Id).ShouldBe(testResourceClaims.Select(x => x.ResourceClaimId), true);
        results.All(x => x.Actions == null).ShouldBe(true);
        //Assert parent Resource Claims
        results.Count(x => x.ParentId.Equals(0)).ShouldBe(parentResourceNames.Count);
        results.Count(x => x.Name.StartsWith(parentPrefix)).ShouldBe(parentResourceNames.Count);
        //Assert child Resource Claims
        results.Count(x => x.Name.StartsWith(childPrefix)).ShouldBe(parentResourceNames.Count * childrenResourceNames.Count);
        //Assert grandchild Resource Claims
        results.Count(x => x.Name.StartsWith(grandChildPrefix)).ShouldBe(parentResourceNames.Count * childrenResourceNames.Count * grandChildResourceNames.Count);

    }


    [Test]
    public void ShouldGetAlphabeticallySortedFlatListForResourceClaims()
    {
        var testClaimSet = new ClaimSet
        { ClaimSetName = "TestClaimSet_test" };
        Save(testClaimSet);
        var testResourceClaims = SetupClaimSetResourceClaimActions(testClaimSet, UniqueNameList("ParentRc", 3), UniqueNameList("ChildRc", 1)).ToList();
        var parentResourceNames = testResourceClaims.Where(x => x.ResourceClaim?.ParentResourceClaim == null)
            .OrderBy(x => x.ResourceClaim.ResourceName).Select(x => x.ResourceClaim?.ResourceName).ToList();
        var childResourceNames = testResourceClaims.Where(x => x.ResourceClaim?.ParentResourceClaim != null)
            .OrderBy(x => x.ResourceClaim?.ResourceName).Select(x => x.ResourceClaim?.ResourceName).ToList();

        List<Infrastructure.ClaimSetEditor.ResourceClaim> results = null;
        using var securityContext = TestContext;
        var query = new GetResourceClaimsAsFlatListQuery(securityContext);
        results = query.Execute().ToList();
        results.Count.ShouldBe(testResourceClaims.Count);
        results.Where(x => x.ParentId == 0).Select(x => x.Name).ToList().ShouldBe(parentResourceNames);
        results.Where(x => x.ParentId != 0).Select(x => x.Name).ToList().ShouldBe(childResourceNames);
    }

}
