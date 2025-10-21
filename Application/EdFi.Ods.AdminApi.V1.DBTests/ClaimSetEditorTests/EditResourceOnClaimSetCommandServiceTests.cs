// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using Moq;
using NUnit.Framework;
using Shouldly;
using Application = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.Application;
using ClaimSet = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.ClaimSet;
using ResourceClaim = EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor.ResourceClaim;

namespace EdFi.Ods.AdminApi.V1.DBTests.ClaimSetEditorTests;

[TestFixture]
public class EditResourceOnClaimSetCommandServiceTests : SecurityDataTestBase
{
    [Test]
    public void ShouldEditParentResourcesOnClaimSet()
    {
        var testApplication = new Application
        {
            ApplicationName = $"Test Application {DateTime.Now:O}"
        };
        Save(testApplication);

        var testClaimSet = new ClaimSet { ClaimSetName = "TestClaimSet", Application = testApplication };
        Save(testClaimSet);

        var parentRcNames = UniqueNameList("ParentRc", 2);
        var testResources = SetupParentResourceClaimsWithChildren(testClaimSet, testApplication, parentRcNames, UniqueNameList("ChildRc", 1));

        var testResource1ToEdit = testResources.Select(x => x.ResourceClaim).Single(x => x.ResourceName == parentRcNames.First());
        var testResource2ToNotEdit = testResources.Select(x => x.ResourceClaim).Single(x => x.ResourceName == parentRcNames.Last());

        var editedResource = new ResourceClaim
        {
            Id = testResource1ToEdit.ResourceClaimId,
            Name = testResource1ToEdit.ResourceName,
            Create = false,
            Read = false,
            Update = true,
            Delete = true,
            ReadChanges = true
        };

        var editResourceOnClaimSetModel = new Mock<IEditResourceOnClaimSetModel>();
        editResourceOnClaimSetModel.Setup(x => x.ClaimSetId).Returns(testClaimSet.ClaimSetId);
        editResourceOnClaimSetModel.Setup(x => x.ResourceClaim).Returns(editedResource);

        using var securityContext = TestContext;
        var command = new EditResourceOnClaimSetCommandService(securityContext);
        command.Execute(editResourceOnClaimSetModel.Object);

        var resourceClaimsForClaimSet = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId);

        var resultResourceClaim1 = resourceClaimsForClaimSet.Single(x => x.Id == editedResource.Id);

        resultResourceClaim1.Create.ShouldBe(editedResource.Create);
        resultResourceClaim1.Read.ShouldBe(editedResource.Read);
        resultResourceClaim1.Update.ShouldBe(editedResource.Update);
        resultResourceClaim1.Delete.ShouldBe(editedResource.Delete);
        resultResourceClaim1.ReadChanges.ShouldBe(editedResource.ReadChanges);

        var resultResourceClaim2 = resourceClaimsForClaimSet.Single(x => x.Id == testResource2ToNotEdit.ResourceClaimId);

        resultResourceClaim2.Create.ShouldBe(true);
        resultResourceClaim2.Read.ShouldBe(false);
        resultResourceClaim2.Update.ShouldBe(false);
        resultResourceClaim2.Delete.ShouldBe(false);
        resultResourceClaim2.ReadChanges.ShouldBe(false);
    }

    [Test]
    public void ShouldEditChildResourcesOnClaimSet()
    {
        var testApplication = new Application
        {
            ApplicationName = $"Test Application {DateTime.Now:O}"
        };
        Save(testApplication);

        var testClaimSet = new ClaimSet { ClaimSetName = "TestClaimSet", Application = testApplication };
        Save(testClaimSet);

        var parentRcNames = UniqueNameList("ParentRc", 1);
        var childRcNames = UniqueNameList("ChildRc", 2);
        var testResources = SetupParentResourceClaimsWithChildren(testClaimSet, testApplication, parentRcNames, childRcNames);

        var testParentResource = testResources.Single(x => x.ResourceClaim.ResourceName == parentRcNames.First());

        var test1ChildResourceClaim = $"{childRcNames.First()}-{parentRcNames.First()}";
        var test2ChildResourceClaim = $"{childRcNames.Last()}-{parentRcNames.First()}";

        using var securityContext = TestContext;
        var testChildResource1ToEdit = securityContext.ResourceClaims.Single(x => x.ResourceName == test1ChildResourceClaim && x.ParentResourceClaimId == testParentResource.ResourceClaim.ResourceClaimId);
        var testChildResource2NotToEdit = securityContext.ResourceClaims.Single(x => x.ResourceName == test2ChildResourceClaim && x.ParentResourceClaimId == testParentResource.ResourceClaim.ResourceClaimId);
        var editedResource = new ResourceClaim
        {
            Id = testChildResource1ToEdit.ResourceClaimId,
            Name = testChildResource1ToEdit.ResourceName,
            Create = false,
            Read = false,
            Update = true,
            Delete = true,
            ReadChanges = true
        };

        var editResourceOnClaimSetModel = new Mock<IEditResourceOnClaimSetModel>();
        editResourceOnClaimSetModel.Setup(x => x.ClaimSetId).Returns(testClaimSet.ClaimSetId);
        editResourceOnClaimSetModel.Setup(x => x.ResourceClaim).Returns(editedResource);

        var command = new EditResourceOnClaimSetCommandService(securityContext);
        command.Execute(editResourceOnClaimSetModel.Object);

        var resourceClaimsForClaimSet = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId);

        var resultParentResourceClaim = resourceClaimsForClaimSet.Single(x => x.Id == testParentResource.ResourceClaim.ResourceClaimId);
        resultParentResourceClaim.Create.ShouldBe(true);
        resultParentResourceClaim.Read.ShouldBe(false);
        resultParentResourceClaim.Update.ShouldBe(false);
        resultParentResourceClaim.Delete.ShouldBe(false);
        resultParentResourceClaim.ReadChanges.ShouldBe(false);

        var resultChildResourceClaim1 =
            resultParentResourceClaim.Children.Single(x => x.Id == editedResource.Id);

        resultChildResourceClaim1.Create.ShouldBe(editedResource.Create);
        resultChildResourceClaim1.Read.ShouldBe(editedResource.Read);
        resultChildResourceClaim1.Update.ShouldBe(editedResource.Update);
        resultChildResourceClaim1.Delete.ShouldBe(editedResource.Delete);
        resultChildResourceClaim1.ReadChanges.ShouldBe(editedResource.ReadChanges);

        var resultChildResourceClaim2 =
            resultParentResourceClaim.Children.Single(x => x.Id == testChildResource2NotToEdit.ResourceClaimId);

        resultChildResourceClaim2.Create.ShouldBe(true);
        resultChildResourceClaim2.Read.ShouldBe(false);
        resultChildResourceClaim2.Update.ShouldBe(false);
        resultChildResourceClaim2.Delete.ShouldBe(false);
        resultChildResourceClaim2.ReadChanges.ShouldBe(false);
    }


    [Test]
    public void ShouldAddParentResourceToClaimSet()
    {
        var testApplication = new Application
        {
            ApplicationName = $"Test Application {DateTime.Now:O}"
        };
        Save(testApplication);

        var testClaimSet = new ClaimSet { ClaimSetName = "TestClaimSet", Application = testApplication };
        Save(testClaimSet);

        var parentRcNames = UniqueNameList("Parent", 1);
        var testResources = SetupResourceClaims(testApplication, parentRcNames, UniqueNameList("child", 1));
        var testResourceToAdd = testResources.Single(x => x.ResourceName == parentRcNames.First());
        var resourceToAdd = new ResourceClaim()
        {
            Id = testResourceToAdd.ResourceClaimId,
            Name = testResourceToAdd.ResourceName,
            Create = true,
            Read = false,
            Update = true,
            Delete = false,
            ReadChanges = false
        };
        var existingResources = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId);

        var editResourceOnClaimSetModel = new EditResourceOnClaimSetModel
        {
            ClaimSetId = testClaimSet.ClaimSetId,
            ResourceClaim = resourceToAdd
        };

        using var securityContext = TestContext;
        var command = new EditResourceOnClaimSetCommandService(securityContext);
        command.Execute(editResourceOnClaimSetModel);

        var resourceClaimsForClaimSet = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId);

        var resultResourceClaim1 = resourceClaimsForClaimSet.Single(x => x.Name == testResourceToAdd.ResourceName);

        resultResourceClaim1.Create.ShouldBe(resourceToAdd.Create);
        resultResourceClaim1.Read.ShouldBe(resourceToAdd.Read);
        resultResourceClaim1.Update.ShouldBe(resourceToAdd.Update);
        resultResourceClaim1.Delete.ShouldBe(resourceToAdd.Delete);
        resultResourceClaim1.ReadChanges.ShouldBe(resourceToAdd.ReadChanges);
    }

    [Test]
    public void ShouldAddChildResourcesToClaimSet()
    {
        var testApplication = new Application
        {
            ApplicationName = $"Test Application {DateTime.Now:O}"
        };
        Save(testApplication);

        var testClaimSet = new ClaimSet { ClaimSetName = "TestClaimSet", Application = testApplication };
        Save(testClaimSet);

        var parentRcNames = UniqueNameList("Parent", 1);
        var childRcNames = UniqueNameList("Child", 1);
        var testResources = SetupResourceClaims(testApplication, parentRcNames, childRcNames);

        var testParentResource1 = testResources.Single(x => x.ResourceName == parentRcNames.First());
        var childRcToTest = $"{childRcNames.First()}-{parentRcNames.First()}";

        using var securityContext = TestContext;
        var testChildResource1ToAdd = securityContext.ResourceClaims.Single(x => x.ResourceName == childRcToTest && x.ParentResourceClaimId == testParentResource1.ResourceClaimId);
        var resourceToAdd = new ResourceClaim()
        {
            Id = testChildResource1ToAdd.ResourceClaimId,
            Name = testChildResource1ToAdd.ResourceName,
            Create = true,
            Read = false,
            Update = true,
            Delete = false,
            ReadChanges = false
        };
        var existingResources = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId);

        var editResourceOnClaimSetModel = new EditResourceOnClaimSetModel
        {
            ClaimSetId = testClaimSet.ClaimSetId,
            ResourceClaim = resourceToAdd
        };

        var command = new EditResourceOnClaimSetCommandService(securityContext);
        command.Execute(editResourceOnClaimSetModel);

        var resourceClaimsForClaimSet = ResourceClaimsForClaimSet(testClaimSet.ClaimSetId);

        var resultChildResourceClaim1 =
            resourceClaimsForClaimSet.Single(x => x.Name == testChildResource1ToAdd.ResourceName);

        resultChildResourceClaim1.Create.ShouldBe(resourceToAdd.Create);
        resultChildResourceClaim1.Read.ShouldBe(resourceToAdd.Read);
        resultChildResourceClaim1.Update.ShouldBe(resourceToAdd.Update);
        resultChildResourceClaim1.Delete.ShouldBe(resourceToAdd.Delete);
        resultChildResourceClaim1.ReadChanges.ShouldBe(resourceToAdd.ReadChanges);
    }
}
