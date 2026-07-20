// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Features;
using EdFi.Ods.AdminApi.Features.ClaimSets;
using EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.ClaimSets;

[TestFixture]
public class AddClaimSetValidatorTests
{
    [Test]
    public void Should_Have_Error_When_Name_Already_Exists()
    {
        var validator = CreateValidator(new ClaimSet { Id = 1, Name = "Existing" });
        var request = new AddClaimSet.AddClaimSetRequest { Name = "Existing" };

        var result = validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.Name)
            && x.ErrorMessage == FeatureConstants.ClaimSetAlreadyExistsMessage)
            .ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_Name_Exceeds_Maximum_Length()
    {
        var validator = CreateValidator();
        var request = new AddClaimSet.AddClaimSetRequest { Name = new string('a', 256) };

        var result = validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.Name)
            && x.ErrorMessage == FeatureConstants.ClaimSetNameMaxLengthMessage)
            .ShouldBeTrue();
    }

    [Test]
    public void Should_Not_Have_Error_When_Name_Is_Unique()
    {
        var validator = CreateValidator(new ClaimSet { Id = 1, Name = "Existing" });
        var request = new AddClaimSet.AddClaimSetRequest { Name = "NewClaimSet" };

        var result = validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.Name)).ShouldBeFalse();
    }

    private static AddClaimSet.Validator CreateValidator(params ClaimSet[] claimSets)
    {
        var fakeGetAllClaimSetsQuery = A.Fake<IGetAllClaimSetsQuery>();
        A.CallTo(() => fakeGetAllClaimSetsQuery.Execute())
            .Returns(new List<ClaimSet>(claimSets));

        return new AddClaimSet.Validator(fakeGetAllClaimSetsQuery);
    }
}
