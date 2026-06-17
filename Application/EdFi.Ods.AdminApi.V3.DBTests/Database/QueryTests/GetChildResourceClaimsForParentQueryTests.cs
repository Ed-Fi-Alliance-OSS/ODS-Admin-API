// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;
using System.Linq;

namespace EdFi.Ods.AdminApi.V3.DBTests.Database.QueryTests;

[TestFixture]
public class GetChildResourceClaimsForParentQueryTests : SecurityDataTestBase
{
    [Test]
    public void ShouldGetResourceClaims()
    {
        var parentRcs = UniqueNameList("Parent", 2);

        var childRcs = UniqueNameList("Child", 1);

        var testResourceClaims = SetupResourceClaims(parentRcs, childRcs);

        var testParentResource = testResourceClaims.Single(x => x.ResourceName == parentRcs.First());

        ResourceClaim[] results = null;
        Transaction(securityContext =>
        {
            var query = new GetChildResourceClaimsForParentQuery(securityContext);

            results = query.Execute(testParentResource.ResourceClaimId).ToArray();
        });

        Transaction(securityContext =>
        {
            var testChildResourceClaims = securityContext.ResourceClaims.Where(x =>
                x.ParentResourceClaimId == testParentResource.ResourceClaimId);

            results.Length.ShouldBe(testChildResourceClaims.Count());
            results.Select(x => x.Name).ShouldBe(testChildResourceClaims.Select(x => x.ResourceName), true);
            results.Select(x => x.Id).ShouldBe(testChildResourceClaims.Select(x => x.ResourceClaimId), true);
            results.All(x => x.Actions == null).ShouldBeTrue();
            results.All(x => x.ParentId.Equals(testParentResource.ResourceClaimId)).ShouldBe(true);
        });
    }
}


