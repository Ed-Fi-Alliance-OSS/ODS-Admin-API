// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.DBTests.ClaimSetEditorTests;

[TestFixture]
public class ImportClaimSetValidatorTests : SecurityDataTestBase
{
    private ImportClaimSet.Validator CreateValidator()
    {
        using var securityContext = TestContext;
        var getAllClaimSetsQuery = new GetAllClaimSetsQuery(securityContext, Testing.GetAppSettings());
        var getResourceClaimsAsFlatListQuery = new GetResourceClaimsAsFlatListQuery(securityContext);
        var getAllAuthorizationStrategiesQuery = new GetAllAuthorizationStrategiesQuery(securityContext);
        var getAllActionsQuery = new GetAllActionsQuery(securityContext, Testing.GetAppSettings());
        return new ImportClaimSet.Validator(
            getAllClaimSetsQuery,
            getResourceClaimsAsFlatListQuery,
            getAllAuthorizationStrategiesQuery,
            getAllActionsQuery);
    }

    [Test]
    public void Validator_Should_Fail_When_Name_Contains_Whitespace()
    {
        var validator = CreateValidator();

        var request = new ImportClaimSet.ImportClaimSetRequest { Name = "imported claimset" };
        var result = validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("white space"));
    }

    [Test]
    public void Validator_Should_Pass_When_Name_Has_No_Whitespace()
    {
        var validator = CreateValidator();

        var request = new ImportClaimSet.ImportClaimSetRequest { Name = "importedclaimset" };
        var result = validator.Validate(request);

        result.Errors.ShouldNotContain(e => e.ErrorMessage.Contains("white space"));
    }
}
