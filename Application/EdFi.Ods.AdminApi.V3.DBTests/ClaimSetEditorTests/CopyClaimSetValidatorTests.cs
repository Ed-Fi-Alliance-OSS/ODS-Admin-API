// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.V3.DBTests.ClaimSetEditorTests;

[TestFixture]
public class CopyClaimSetValidatorTests : SecurityDataTestBase
{
    [Test]
    public void Validator_Should_Fail_When_Name_Contains_Whitespace()
    {
        var testClaimSet = new ClaimSet { ClaimSetName = "OriginalClaimSet" };
        Save(testClaimSet);

        using var securityContext = TestContext;
        var getAllClaimSetsQuery = new GetAllClaimSetsQuery(securityContext, Testing.GetAppSettings());
        var getClaimSetByIdQuery = new GetClaimSetByIdQuery(securityContext);
        var validator = new CopyClaimSet.Validator(getAllClaimSetsQuery, getClaimSetByIdQuery);

        var request = new CopyClaimSet.CopyClaimSetRequest
        {
            OriginalId = testClaimSet.ClaimSetId,
            Name = "copied claimset"
        };
        var result = validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("white space"));
    }

    [Test]
    public void Validator_Should_Pass_When_Name_Has_No_Whitespace()
    {
        var testClaimSet = new ClaimSet { ClaimSetName = "OriginalClaimSet2" };
        Save(testClaimSet);

        using var securityContext = TestContext;
        var getAllClaimSetsQuery = new GetAllClaimSetsQuery(securityContext, Testing.GetAppSettings());
        var getClaimSetByIdQuery = new GetClaimSetByIdQuery(securityContext);
        var validator = new CopyClaimSet.Validator(getAllClaimSetsQuery, getClaimSetByIdQuery);

        var request = new CopyClaimSet.CopyClaimSetRequest
        {
            OriginalId = testClaimSet.ClaimSetId,
            Name = "copiedclaimset"
        };
        var result = validator.Validate(request);

        result.Errors.ShouldNotContain(e => e.ErrorMessage.Contains("white space"));
    }
}
