// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Features.Applications;
using EdFi.Ods.AdminApi.Infrastructure.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Applications;

#nullable enable

public class EditApplicationModelStub : IEditApplicationModel
{
    public int Id { get; set; }
    public string? ApplicationName { get; set; }
    public int VendorId { get; set; }
    public string? ClaimSetName { get; set; }
    public IEnumerable<int>? ProfileIds { get; set; }
    public IEnumerable<long>? EducationOrganizationIds { get; set; }
    public IEnumerable<int>? OdsInstanceIds { get; set; }
    public bool? Enabled { get; set; }
}

[TestFixture]
public class EditApplicationValidatorTests
{
    private EditApplication.Validator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new EditApplication.Validator();
    }

    [Test]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var model = ValidModel();
        model.Id = 0;

        var result = _validator.Validate(model);

        result.Errors.Any(x => x.PropertyName == nameof(model.Id)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_ApplicationName_Is_Empty()
    {
        var model = ValidModel();
        model.ApplicationName = string.Empty;

        var result = _validator.Validate(model);

        result.Errors.Any(x => x.PropertyName == nameof(model.ApplicationName)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_ApplicationName_Exceeds_Max_Length()
    {
        var model = ValidModel();
        model.ApplicationName = new string('A', ValidationConstants.MaximumApplicationNameLength + 1);

        var result = _validator.Validate(model);

        result.Errors.Any(x => x.PropertyName == nameof(model.ApplicationName)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_ClaimSetName_Is_Empty()
    {
        var model = ValidModel();
        model.ClaimSetName = string.Empty;

        var result = _validator.Validate(model);

        result.Errors.Any(x => x.PropertyName == nameof(model.ClaimSetName)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_EducationOrganizationIds_Is_Empty()
    {
        var model = ValidModel();
        model.EducationOrganizationIds = Array.Empty<long>();

        var result = _validator.Validate(model);

        result.Errors.Any(x => x.PropertyName == nameof(model.EducationOrganizationIds)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_OdsInstanceIds_Is_Empty()
    {
        var model = ValidModel();
        model.OdsInstanceIds = Array.Empty<int>();

        var result = _validator.Validate(model);

        result.Errors.Any(x => x.PropertyName == nameof(model.OdsInstanceIds)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_VendorId_Is_Not_Positive()
    {
        var model = ValidModel();
        model.VendorId = 0;

        var result = _validator.Validate(model);

        result.Errors.Any(x => x.PropertyName == nameof(model.VendorId)).ShouldBeTrue();
    }

    [Test]
    public void Should_Not_Have_Error_For_Valid_Model()
    {
        var result = _validator.Validate(ValidModel());

        result.IsValid.ShouldBeTrue();
    }

    private static EditApplicationModelStub ValidModel()
    {
        return new EditApplicationModelStub
        {
            Id = 1,
            ApplicationName = "Test Application",
            VendorId = 1,
            ClaimSetName = "TestClaimSet",
            EducationOrganizationIds = new long[] { 1L },
            OdsInstanceIds = new[] { 1 }
        };
    }
}
