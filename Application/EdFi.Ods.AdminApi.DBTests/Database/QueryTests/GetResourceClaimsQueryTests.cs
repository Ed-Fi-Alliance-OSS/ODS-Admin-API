// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;
using ResourceClaim = EdFi.Security.DataAccess.Models.ResourceClaim;

namespace EdFi.Ods.AdminApi.DBTests.Database.QueryTests;

[TestFixture]
public class GetResourceClaimsQueryTests : SecurityDataTestBase
{
    [Test]
    public void ShouldGetResourceClaims()
    {
        var testResourceClaims = SetupResourceClaims();

        Infrastructure.ClaimSetEditor.ResourceClaim[] results = null;
        using var securityContext = TestContext;
        var query = new GetResourceClaimsQuery(securityContext, Testing.GetAppSettings());
        results = query.Execute().ToArray();
        results.Length.ShouldBe(testResourceClaims.Count);
        results.Select(x => x.Name).ShouldBe(testResourceClaims.Select(x => x.ResourceName), true);
        results.Select(x => x.Id).ShouldBe(testResourceClaims.Select(x => x.ResourceClaimId), true);
        results.All(x => x.Actions == null).ShouldBe(true);
        results.All(x => x.ParentId.Equals(0)).ShouldBe(true);
        results.All(x => x.ParentName == null).ShouldBe(true);
        results.All(x => x.Children.Count == 0).ShouldBe(true);
    }

    [Test]
    public void ShouldGetResourceClaimsWithOffset()
    {
        var skip = 3;
        var testResourceClaims = SetupResourceClaims();
        var testResourceClaimsResult = testResourceClaims.Skip(skip);

        Infrastructure.ClaimSetEditor.ResourceClaim[] results = null;
        using var securityContext = TestContext;
        var query = new GetResourceClaimsQuery(securityContext, Testing.GetAppSettings());
        results = query.Execute(new CommonQueryParams(skip, Testing.DefaultPageSizeLimit), null, null).ToArray();
        results.Length.ShouldBe(2);
        results.Select(x => x.Name).ShouldBe(testResourceClaimsResult.Select(x => x.ResourceName), true);
        results.Select(x => x.Id).ShouldBe(testResourceClaimsResult.Select(x => x.ResourceClaimId), true);
        results.All(x => x.Actions == null).ShouldBe(true);
        results.All(x => x.ParentId.Equals(0)).ShouldBe(true);
        results.All(x => x.ParentName == null).ShouldBe(true);
        results.All(x => x.Children.Count == 0).ShouldBe(true);
    }

    [Test]
    public void ShouldGetResourceClaimsWithLimit()
    {
        var limit = 3;
        var testResourceClaims = SetupResourceClaims();
        var testResourceClaimsResult = testResourceClaims.Take(limit);

        Infrastructure.ClaimSetEditor.ResourceClaim[] results = null;
        using var securityContext = TestContext;
        var query = new GetResourceClaimsQuery(securityContext, Testing.GetAppSettings());
        results = query.Execute(new CommonQueryParams(Testing.DefaultPageSizeOffset, limit), null, null).ToArray();
        results.Length.ShouldBe(3);
        results.Select(x => x.Name).ShouldBe(testResourceClaimsResult.Select(x => x.ResourceName), true);
        results.Select(x => x.Id).ShouldBe(testResourceClaimsResult.Select(x => x.ResourceClaimId), true);
        results.All(x => x.Actions == null).ShouldBe(true);
        results.All(x => x.ParentId.Equals(0)).ShouldBe(true);
        results.All(x => x.ParentName == null).ShouldBe(true);
        results.All(x => x.Children.Count == 0).ShouldBe(true);
    }

    [Test]
    public void ShouldGetResourceClaimsWithOffsetAndLimit()
    {
        var offset = 2;
        var limit = 2;
        var testResourceClaims = SetupResourceClaims();
        var testResourceClaimsResult = testResourceClaims.Skip(offset).Take(limit);

        Infrastructure.ClaimSetEditor.ResourceClaim[] results = null;
        using var securityContext = TestContext;
        var query = new GetResourceClaimsQuery(securityContext, Testing.GetAppSettings());
        results = query.Execute(new CommonQueryParams(offset, limit), null, null).ToArray();
        results.Length.ShouldBe(2);
        results.Select(x => x.Name).ShouldBe(testResourceClaimsResult.Select(x => x.ResourceName), true);
        results.Select(x => x.Id).ShouldBe(testResourceClaimsResult.Select(x => x.ResourceClaimId), true);
        results.All(x => x.Actions == null).ShouldBe(true);
        results.All(x => x.ParentId.Equals(0)).ShouldBe(true);
        results.All(x => x.ParentName == null).ShouldBe(true);
        results.All(x => x.Children.Count == 0).ShouldBe(true);
    }

    [Test]
    public void ShouldGetResourceClaimsWithId()
    {
        var name = $"TestResourceClaim{2:N}";
        var testResourceClaims = SetupResourceClaims();
        var testResourceClaimsResult = testResourceClaims.First(c => c.ResourceName == name);

        Infrastructure.ClaimSetEditor.ResourceClaim[] results = null;
        using var securityContext = TestContext;
        var query = new GetResourceClaimsQuery(securityContext, Testing.GetAppSettings());
        results = query.Execute(new CommonQueryParams(), testResourceClaimsResult.ResourceClaimId, null).ToArray();
        results.Length.ShouldBe(1);
        results.First().Id.ShouldBe(testResourceClaimsResult.ResourceClaimId);
    }

    [Test]
    public void ShouldGetResourceClaimsWithName()
    {
        var name = $"TestResourceClaim{2:N}";
        var testResourceClaims = SetupResourceClaims();
        var testResourceClaimsResult = testResourceClaims.Where(c => c.ResourceName == name);

        Infrastructure.ClaimSetEditor.ResourceClaim[] results = null;
        using var securityContext = TestContext;
        var query = new GetResourceClaimsQuery(securityContext, Testing.GetAppSettings());
        results = query.Execute(new CommonQueryParams(), null, name).ToArray();
        results.Length.ShouldBe(1);
        results.First().Name.ShouldBe(testResourceClaimsResult.First().ResourceName);
    }

    private IReadOnlyCollection<ResourceClaim> SetupResourceClaims(int resourceClaimCount = 5)
    {
        var resourceClaims = new List<ResourceClaim>();
        foreach (var index in Enumerable.Range(1, resourceClaimCount))
        {
            var resourceClaim = new ResourceClaim
            {
                ClaimName = $"TestResourceClaim{index:N}",
                ResourceName = $"TestResourceClaim{index:N}",
            };
            resourceClaims.Add(resourceClaim);
        }

        Save(resourceClaims.Cast<object>().ToArray());

        return resourceClaims;
    }
}
