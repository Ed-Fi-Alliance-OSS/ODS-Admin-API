// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using Moq;
using NUnit.Framework;
using Shouldly;
using Application = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.Application;
using ClaimSet = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.V1.DBTests.ClaimSetEditorTests;

[TestFixture]
public class DeleteClaimSetCommandServiceTests : SecurityDataTestBase
{

    [Test]
    public void ShouldDeleteClaimSet()
    {
        var testApplication = new Application
        {
            ApplicationName = $"Test Application {DateTime.Now:O}"
        };
        Save(testApplication);

        var testClaimSetToDelete = new ClaimSet
        { ClaimSetName = "TestClaimSet_Delete", Application = testApplication };
        Save(testClaimSetToDelete);
        SetupParentResourceClaimsWithChildren(testClaimSetToDelete, testApplication, UniqueNameList("ParentRc", 3), UniqueNameList("ChildRc", 1));

        var testClaimSetToPreserve = new ClaimSet
        { ClaimSetName = "TestClaimSet_Preserve", Application = testApplication };
        Save(testClaimSetToPreserve);
        var resourceClaimsForPreservedClaimSet = SetupParentResourceClaimsWithChildren(testClaimSetToPreserve, testApplication, UniqueNameList("ParentRc", 3),
            UniqueNameList("ChildRc", 1));

        var deleteModel = new Mock<IDeleteClaimSetModel>();
        deleteModel.Setup(x => x.Name).Returns(testClaimSetToDelete.ClaimSetName);
        deleteModel.Setup(x => x.Id).Returns(testClaimSetToDelete.ClaimSetId);

        using var securityContext = TestContext;
        var command = new DeleteClaimSetCommandService(securityContext);
        command.Execute(deleteModel.Object);
        var deletedClaimSet = securityContext.ClaimSets.SingleOrDefault(x => x.ClaimSetId == testClaimSetToDelete.ClaimSetId);
        deletedClaimSet.ShouldBeNull();
        var deletedClaimSetResourceActions = securityContext.ClaimSetResourceClaimActions.Count(x => x.ClaimSet.ClaimSetId == testClaimSetToDelete.ClaimSetId);
        deletedClaimSetResourceActions.ShouldBe(0);

        var preservedClaimSet = securityContext.ClaimSets.Single(x => x.ClaimSetId == testClaimSetToPreserve.ClaimSetId);
        preservedClaimSet.ClaimSetName.ShouldBe(testClaimSetToPreserve.ClaimSetName);

        var results = ResourceClaimsForClaimSet(testClaimSetToPreserve.ClaimSetId);

        var testParentResourceClaimsForId =
            resourceClaimsForPreservedClaimSet.Where(x => x.ClaimSet.ClaimSetId == testClaimSetToPreserve.ClaimSetId && x.ResourceClaim.ParentResourceClaim == null).Select(x => x.ResourceClaim).ToArray();

        results.Count.ShouldBe(testParentResourceClaimsForId.Length);
        results.Select(x => x.Name).ShouldBe(testParentResourceClaimsForId.Select(x => x.ResourceName), true);
        results.Select(x => x.Id).ShouldBe(testParentResourceClaimsForId.Select(x => x.ResourceClaimId), true);
        results.All(x => x.Create).ShouldBe(true);

        foreach (var testParentResourceClaim in testParentResourceClaimsForId)
        {
            var testChildren = securityContext.ResourceClaims.Where(x =>
                x.ParentResourceClaimId == testParentResourceClaim.ResourceClaimId).ToList();
            var parentResult = results.First(x => x.Id == testParentResourceClaim.ResourceClaimId);
            parentResult.Children.Select(x => x.Name).ShouldBe(testChildren.Select(x => x.ResourceName), true);
            parentResult.Children.Select(x => x.Id).ShouldBe(testChildren.Select(x => x.ResourceClaimId), true);
            parentResult.Children.All(x => x.Create).ShouldBe(true);
        }
    }

    [Test]
    public void ShouldThrowExceptionOnEditSystemReservedClaimSet()
    {
        var testApplication = new Application
        {
            ApplicationName = $"Test Application {DateTime.Now:O}"
        };
        Save(testApplication);

        var systemReservedClaimSet = new ClaimSet { ClaimSetName = "SIS Vendor", Application = testApplication, IsEdfiPreset = true };
        Save(systemReservedClaimSet);

        var deleteModel = new Mock<IDeleteClaimSetModel>();
        deleteModel.Setup(x => x.Name).Returns(systemReservedClaimSet.ClaimSetName);
        deleteModel.Setup(x => x.Id).Returns(systemReservedClaimSet.ClaimSetId);
        using var securityContext = TestContext;
        var exception = Assert.Throws<AdminApiException>(() =>
        {
            var command = new DeleteClaimSetCommandService(securityContext);
            command.Execute(deleteModel.Object);
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe($"Claim set({systemReservedClaimSet.ClaimSetName}) is system reserved.Can not be deleted.");
    }

    // TODO: move these to UnitTests, using appropriate validator from the API project
    //[Test]
    //public void ShouldNotDeleteClaimSetIfNotEditable()
    //{
    //    var testApplication = new Application
    //    {
    //        ApplicationName = "TestApplication1"
    //    };
    //    Save(testApplication);

    //    var testClaimSet = new ClaimSet {ClaimSetName = $"TestClaimSet{DateTime.Now:O}", Application = testApplication};
    //    Save(testClaimSet);

    //    var claimSetToDelete = new DeleteClaimSetModel()
    //    {
    //        Name = testClaimSet.ClaimSetName,
    //        Id = testClaimSet.ClaimSetId,
    //        IsEditable = false
    //    };

    //    using var securityContext = TestContext;
    //    var getClaimSetByIdQuery = ClaimSetByIdQuery(securityContext);
    //    var validator = new DeleteClaimSetModelValidator(getClaimSetByIdQuery);
    //    var validationResults = validator.Validate(claimSetToDelete);
    //    validationResults.IsValid.ShouldBe(false);
    //    validationResults.Errors.Single().ErrorMessage.ShouldBe("Only user created claim sets can be deleted");
    //}

    //[Test]
    //public void ShouldNotDeleteClaimSetIfNotAnExistingId()
    //{
    //    var testApplication = new Application
    //    {
    //        ApplicationName = "TestApplication2"
    //    };
    //    Save(testApplication);

    //    var testClaimSet = new ClaimSet { ClaimSetName = $"TestClaimSet{DateTime.Now:O}", Application = testApplication };
    //    Save(testClaimSet);

    //    var claimSetToDelete = new DeleteClaimSetModel()
    //    {
    //        Name = testClaimSet.ClaimSetName,
    //        Id = 99,
    //        IsEditable = true
    //    };
    //    using var securityContext = TestContext;
    //    var getClaimSetByIdQuery = ClaimSetByIdQuery(securityContext);
    //    var validator = new DeleteClaimSetModelValidator(getClaimSetByIdQuery);
    //    var validationResults = validator.Validate(claimSetToDelete);
    //    validationResults.IsValid.ShouldBe(false);
    //    validationResults.Errors.Single().ErrorMessage.ShouldBe("No such claim set exists in the database");
    //}

    //[Test]
    //public void ShouldNotDeleteClaimSetHasAnAssociatedApplication()
    //{
    //    var testApplication = new Application
    //    {
    //        ApplicationName = "TestApplication3"
    //    };
    //    Save(testApplication);

    //    var testClaimSet = new ClaimSet { ClaimSetName = $"TestClaimSet{DateTime.Now:O}", Application = testApplication };
    //    Save(testClaimSet);

    //    var claimSetToDelete = new DeleteClaimSetModel()
    //    {
    //        Name = testClaimSet.ClaimSetName,
    //        Id = testClaimSet.ClaimSetId,
    //        IsEditable = true,
    //        VendorApplicationCount = 1
    //    };

    //    using var securityContext = TestContext;
    //    var getClaimSetByIdQuery = ClaimSetByIdQuery(securityContext);
    //    var validator = new DeleteClaimSetModelValidator(getClaimSetByIdQuery);
    //    var validationResults = validator.Validate(claimSetToDelete);
    //    validationResults.IsValid.ShouldBe(false);
    //    validationResults.Errors.Single().ErrorMessage.ShouldBe($"Cannot delete this claim set. This claim set has {claimSetToDelete.VendorApplicationCount} associated application(s).");
    //}

    //private GetClaimSetByIdQuery ClaimSetByIdQuery(ISecurityContext securityContext) => new GetClaimSetByIdQuery(new StubOdsSecurityModelVersionResolver.V6(),
    //                null, new GetClaimSetByIdQueryV6Service(securityContext));
}
