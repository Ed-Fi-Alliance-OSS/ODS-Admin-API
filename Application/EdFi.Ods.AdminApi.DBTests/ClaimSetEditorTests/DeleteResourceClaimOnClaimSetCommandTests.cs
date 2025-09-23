// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using NUnit.Framework;
using Shouldly;
using System.Linq;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.DBTests.ClaimSetEditorTests;

[TestFixture]
public class DeleteResourceClaimOnClaimSetCommandTests : SecurityDataTestBase
{
    [Test]
    public void ShouldDeleteResourceClaimOnClaimSet()
    {
        var testClaimSet = new ClaimSet { ClaimSetName = "TestClaimSet" };
        Save(testClaimSet);

        var parentRcNames = UniqueNameList("ParentRc", 2);
        var testResources = SetupClaimSetResourceClaimActions(testClaimSet, parentRcNames, UniqueNameList("ChildRc", 1));

        using var securityContext = TestContext;
        var command = new DeleteResouceClaimOnClaimSetCommand(securityContext);
        command.Execute(testClaimSet.ClaimSetId, testResources.First().ResourceClaimId);

        var resourceClaimsForClaimSet = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId);

        resourceClaimsForClaimSet.Count.ShouldBeLessThan(testResources.Count);
    }
}
