// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Security.DataAccess.Contexts;
using NUnit.Framework;
using Shouldly;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.V3.DBTests.ClaimSetEditorTests;

[TestFixture]
public class GetClaimSetByIdQueryTests : SecurityDataTestBase
{
    [Test]
    public void ShouldGetClaimSetById()
    {
        var testClaimSet = new ClaimSet
        {
            ClaimSetName = "TestClaimSet",
            ForApplicationUseOnly = false,
            IsEdfiPreset = false
        };
        Save(testClaimSet);

        using var securityContext = TestContext;
        var query = new GetClaimSetByIdQuery(securityContext);
        var result = query.Execute(testClaimSet.ClaimSetId);
        result.Name.ShouldBe(testClaimSet.ClaimSetName);
        result.Id.ShouldBe(testClaimSet.ClaimSetId);
        result.IsEditable.ShouldBe(true);
    }

    [Test]
    public void ShouldGetNonEditableClaimSetById()
    {
        var systemReservedClaimSet = new ClaimSet
        {
            ClaimSetName = "SystemReservedClaimSet",
            ForApplicationUseOnly = true
        };
        Save(systemReservedClaimSet);

        var edfiPresetClaimSet = new ClaimSet
        {
            ClaimSetName = "EdfiPresetClaimSet",
            ForApplicationUseOnly = false,
            IsEdfiPreset = true
        };
        Save(edfiPresetClaimSet);

        using var securityContext = TestContext;
        var query = new GetClaimSetByIdQuery(securityContext);
        var result = query.Execute(systemReservedClaimSet.ClaimSetId);
        result.Name.ShouldBe(systemReservedClaimSet.ClaimSetName);
        result.Id.ShouldBe(systemReservedClaimSet.ClaimSetId);
        result.IsEditable.ShouldBe(false);

        result = query.Execute(edfiPresetClaimSet.ClaimSetId);

        result.Name.ShouldBe(edfiPresetClaimSet.ClaimSetName);
        result.Id.ShouldBe(edfiPresetClaimSet.ClaimSetId);
        result.IsEditable.ShouldBe(false);
    }

    [Test]
    public void ShouldThrowExceptionForNonExistingClaimSetId()
    {
        const int NonExistingClaimSetId = 1;

        using var securityContext = TestContext;
        EnsureZeroClaimSets(securityContext);

        var exception = Assert.Throws<NotFoundException<int>>(() =>
        {
            var query = new GetClaimSetByIdQuery(securityContext);
            query.Execute(NonExistingClaimSetId);
        });
        exception.ShouldNotBeNull();
        exception.Message.ShouldBe("Not found: claimset with ID 1. It may have been recently deleted.");

        static void EnsureZeroClaimSets(ISecurityContext database)
        {
            foreach (var entity in database.ClaimSets)
                database.ClaimSets.Remove(entity);
            database.SaveChanges();
        }
    }
}



