// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApp.Management.ClaimSetEditor;
using Moq;
using NUnit.Framework;
using Shouldly;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.DBTests.ClaimSetEditorTests;

[TestFixture]
public class CopyClaimSetCommandTests : SecurityDataTestBase
{
    [Test]
    public void ShouldCopyClaimSet()
    {
        var testClaimSet = new ClaimSet { ClaimSetName = "TestClaimSet" };
        Save(testClaimSet);

        var testResourceClaims = SetupClaimSetResourceClaimActions(testClaimSet,
           UniqueNameList("ParentRc", 3), UniqueNameList("ChildRc", 1));

        var newClaimSet = new Mock<ICopyClaimSetModel>();
        newClaimSet.Setup(x => x.Name).Returns("TestClaimSet_Copy");
        newClaimSet.Setup(x => x.OriginalId).Returns(testClaimSet.ClaimSetId);

        var copyClaimSetId = 0;
        ClaimSet copiedClaimSet = null;
        using var securityContext = TestContext;
        var command = new CopyClaimSetCommand(securityContext);
        copyClaimSetId = command.Execute(newClaimSet.Object);
        copiedClaimSet = securityContext.ClaimSets.Single(x => x.ClaimSetId == copyClaimSetId);

        copiedClaimSet.ClaimSetName.ShouldBe(newClaimSet.Object.Name);
        copiedClaimSet.ForApplicationUseOnly.ShouldBe(false);
        copiedClaimSet.IsEdfiPreset.ShouldBe(false);

        var results = ResourceClaimsForClaimSet(copiedClaimSet.ClaimSetId).ToList();

        var testParentResourceClaimsForId =
            testResourceClaims.Where(x => x.ClaimSet.ClaimSetId == testClaimSet.ClaimSetId && x.ResourceClaim.ParentResourceClaim == null).Select(x => x.ResourceClaim).ToArray();

        results.Count.ShouldBe(testParentResourceClaimsForId.Length);
        results.Select(x => x.Name).ShouldBe(testParentResourceClaimsForId.Select(x => x.ResourceName), true);
        results.Select(x => x.Id).ShouldBe(testParentResourceClaimsForId.Select(x => x.ResourceClaimId), true);
        results.All(x => x.Actions.All(x => x.Name.Equals("Create") && x.Enabled)).ShouldBe(true);

        foreach (var testParentResourceClaim in testParentResourceClaimsForId)
        {
            var testChildren = securityContext.ResourceClaims.Where(x =>
                x.ParentResourceClaimId == testParentResourceClaim.ResourceClaimId).ToList();
            var parentResult = results.First(x => x.Id == testParentResourceClaim.ResourceClaimId);
            parentResult.Children.Select(x => x.Name).ShouldBe(testChildren.Select(x => x.ResourceName), true);
            parentResult.Children.Select(x => x.Id).ShouldBe(testChildren.Select(x => x.ResourceClaimId), true);
            parentResult.Children.All(x => x.Actions.All(x => x.Name.Equals("Create") && x.Enabled)).ShouldBe(true);
        }
    }
}
