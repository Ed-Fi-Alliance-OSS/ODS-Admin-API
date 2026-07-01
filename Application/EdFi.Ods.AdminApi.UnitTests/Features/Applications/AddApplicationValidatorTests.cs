// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.Features.Applications;
using EdFi.Ods.AdminApi.Infrastructure.Commands;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Applications;

[TestFixture]
public class AddApplicationValidatorTests
{
    private AddApplication.Validator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new AddApplication.Validator();
    }

    [Test]
    public void Should_Have_Error_When_ApplicationName_Is_Empty()
    {
        var request = ValidRequest();
        request.ApplicationName = string.Empty;

        var result = _validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.ApplicationName)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_ApplicationName_Exceeds_Max_Length()
    {
        var request = ValidRequest();
        request.ApplicationName = new string('A', ValidationConstants.MaximumApplicationNameLength + 1);

        var result = _validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.ApplicationName)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_ClaimSetName_Is_Empty()
    {
        var request = ValidRequest();
        request.ClaimSetName = string.Empty;

        var result = _validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.ClaimSetName)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_EducationOrganizationIds_Is_Empty()
    {
        var request = ValidRequest();
        request.EducationOrganizationIds = Array.Empty<long>();

        var result = _validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.EducationOrganizationIds)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_OdsInstanceIds_Is_Empty()
    {
        var request = ValidRequest();
        request.OdsInstanceIds = Array.Empty<int>();

        var result = _validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.OdsInstanceIds)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_VendorId_Is_Not_Positive()
    {
        var request = ValidRequest();
        request.VendorId = 0;

        var result = _validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.VendorId)).ShouldBeTrue();
    }

    [Test]
    public void Should_Not_Have_Error_For_Valid_Request()
    {
        var result = _validator.Validate(ValidRequest());

        result.IsValid.ShouldBeTrue();
    }

    private static AddApplication.AddApplicationRequest ValidRequest()
    {
        return new AddApplication.AddApplicationRequest
        {
            ApplicationName = "Test Application",
            VendorId = 1,
            ClaimSetName = "TestClaimSet",
            EducationOrganizationIds = new long[] { 1L },
            OdsInstanceIds = new[] { 1 }
        };
    }
}
