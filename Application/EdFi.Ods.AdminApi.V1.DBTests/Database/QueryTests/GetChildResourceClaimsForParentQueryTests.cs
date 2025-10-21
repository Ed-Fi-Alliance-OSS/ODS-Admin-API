// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using NUnit.Framework;
using Shouldly;
using Application = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.Application;

namespace EdFi.Ods.AdminApi.V1.DBTests.Database.QueryTests;

[TestFixture]
public class GetChildResourceClaimsForParentQueryTests : SecurityDataTestBase
{
    [Test]
    public void ShouldGetResourceClaims()
    {
        var testApplication = new Application
        {
            ApplicationName = "TestApplicationName"
        };

        Save(testApplication);

        var parentRcs = UniqueNameList("Parent", 2);

        var childRcs = UniqueNameList("Child", 1);

        var testResourceClaims = SetupResourceClaims(testApplication, parentRcs, childRcs);

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
            results.All(x => x.Create == false).ShouldBe(true);
            results.All(x => x.Delete == false).ShouldBe(true);
            results.All(x => x.Update == false).ShouldBe(true);
            results.All(x => x.Read == false).ShouldBe(true);
            results.All(x => x.ParentId.Equals(testParentResource.ResourceClaimId)).ShouldBe(true);
        });
    }
}
