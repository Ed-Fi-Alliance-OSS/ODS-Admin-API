// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Moq;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.DBTests.ClaimSetEditorTests;

[TestFixture]
public class UpdateResourcesOnClaimSetCommandTests : SecurityDataTestBase
{
    [Test]
    public void ShouldUpdateResourcesOnClaimSet()
    {
        var testClaimSet = new ClaimSet { ClaimSetName = "TestClaimSet" };
        Save(testClaimSet);

        var parentRcNames = UniqueNameList("ParentRc", 2);
        var childName = "ChildRc098";
        var testResources = SetupClaimSetResourceClaimActions(testClaimSet, parentRcNames,
            new List<string> { childName });

        var testParentResource = testResources.Single(x => x.ResourceClaim.ResourceName == parentRcNames.First());
        var secondTestParentResource = testResources.Single(x => x.ResourceClaim.ResourceName == parentRcNames.Last());

        var firstParentChildName = $"{childName}-{parentRcNames.First()}";
        using var securityContext = TestContext;
        var testChildResource1ToEdit = securityContext.ResourceClaims.Single(x => x.ResourceName == firstParentChildName && x.ParentResourceClaimId == testParentResource.ResourceClaim.ResourceClaimId);

        var addedResourceClaimsForClaimSet = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId);
        addedResourceClaimsForClaimSet.Count.ShouldBe(2);
        var secondParentResourceClaim = addedResourceClaimsForClaimSet.Single(x => x.Id == secondTestParentResource.ResourceClaim.ResourceClaimId);
        secondParentResourceClaim.ShouldNotBeNull();

        var updatedParent1 = new ResourceClaim
        {
            Id = testParentResource.ResourceClaim.ResourceClaimId,
            Name = testParentResource.ResourceClaim.ResourceName,
            Actions = new List<ResourceClaimAction>
            {
                new ResourceClaimAction{ Name = "Create", Enabled = false },
                new ResourceClaimAction{ Name = "Read", Enabled = false },
                new ResourceClaimAction{ Name = "Update", Enabled = true },
                new ResourceClaimAction{ Name = "Delete", Enabled = true}
            },
            Children = new List<ResourceClaim> {new ResourceClaim
                {
                    Id = testChildResource1ToEdit.ResourceClaimId,
                    Name = testChildResource1ToEdit.ResourceName,
                      Actions = new List<ResourceClaimAction>
                        {
                            new ResourceClaimAction{ Name = "Create", Enabled = false },
                            new ResourceClaimAction{ Name = "Read", Enabled = false },
                            new ResourceClaimAction{ Name = "Update", Enabled = true },
                            new ResourceClaimAction{ Name = "Delete", Enabled = true}
                        }
                } }
        };

        var updatedResourceClaims = new List<ResourceClaim>
        {
            updatedParent1
        };

        var updateResourcesOnClaimSetModel = new Mock<IUpdateResourcesOnClaimSetModel>();
        updateResourcesOnClaimSetModel.Setup(x => x.ClaimSetId).Returns(testClaimSet.ClaimSetId);
        updateResourcesOnClaimSetModel.Setup(x => x.ResourceClaims).Returns(updatedResourceClaims);

        using var context = CreateDbContext();
        var addOrEditResourcesOnClaimSetCommand = new AddOrEditResourcesOnClaimSetCommand(
            new EditResourceOnClaimSetCommand(context),
            new GetResourceClaimsQuery(context, Testing.GetAppSettings()),
            new OverrideDefaultAuthorizationStrategyCommand(context));

        var command = new UpdateResourcesOnClaimSetCommand(context, addOrEditResourcesOnClaimSetCommand);
        command.Execute(updateResourcesOnClaimSetModel.Object);

        var resourceClaimsForClaimSet = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId);
        resourceClaimsForClaimSet.Count.ShouldBe(1);
        resourceClaimsForClaimSet.First().Name.ShouldBe(testParentResource.ResourceClaim.ResourceName);
        resourceClaimsForClaimSet.First().Children.Count.ShouldBe(1);
        resourceClaimsForClaimSet.First().Children.First().Name.ShouldBe(testChildResource1ToEdit.ResourceName);
    }
}
