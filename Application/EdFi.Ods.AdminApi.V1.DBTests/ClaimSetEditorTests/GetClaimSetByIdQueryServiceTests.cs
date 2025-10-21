// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Net;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using EdFi.Ods.AdminApi.V1.Security.DataAccess.Contexts;
using NUnit.Framework;
using Shouldly;
using Application = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.Application;
using ClaimSet = EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.V1.DBTests.ClaimSetEditorTests;

[TestFixture]
public class GetClaimSetByIdQueryServiceTests : SecurityDataTestBase
{
    [Test]
    public void ShouldGetClaimSetById()
    {
        var testApplication = new Application
        {
            ApplicationName = $"Test Application {DateTime.Now:O}"
        };
        Save(testApplication);

        var testClaimSet = new ClaimSet
        {
            ClaimSetName = "TestClaimSet",
            Application = testApplication,
            ForApplicationUseOnly = false,
            IsEdfiPreset = false
        };
        Save(testClaimSet);

        using var securityContext = TestContext;
        var query = new GetClaimSetByIdQueryService(securityContext);
        var result = query.Execute(testClaimSet.ClaimSetId);
        result.Name.ShouldBe(testClaimSet.ClaimSetName);
        result.Id.ShouldBe(testClaimSet.ClaimSetId);
        result.IsEditable.ShouldBe(true);
    }

    [Test]
    public void ShouldGetNonEditableClaimSetById()
    {
        var testApplication = new Application
        {
            ApplicationName = $"Test Application {DateTime.Now:O}"
        };
        Save(testApplication);

        var systemReservedClaimSet = new ClaimSet
        {
            ClaimSetName = "SystemReservedClaimSet",
            Application = testApplication,
            ForApplicationUseOnly = true
        };
        Save(systemReservedClaimSet);

        var edfiPresetClaimSet = new ClaimSet
        {
            ClaimSetName = "EdfiPresetClaimSet",
            Application = testApplication,
            ForApplicationUseOnly = false,
            IsEdfiPreset = true
        };
        Save(edfiPresetClaimSet);

        using var securityContext = TestContext;
        var query = new GetClaimSetByIdQueryService(securityContext);
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

        var adminApiException = Assert.Throws<AdminApiException>(() =>
        {
            var query = new GetClaimSetByIdQueryService(securityContext);
            query.Execute(NonExistingClaimSetId);
        });
        adminApiException.ShouldNotBeNull();
        adminApiException.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        adminApiException.Message.ShouldBe("No such claim set exists in the database.");

        static void EnsureZeroClaimSets(ISecurityContext database)
        {
            foreach (var entity in database.ClaimSets)
                database.ClaimSets.Remove(entity);
            database.SaveChanges();
        }
    }
}
