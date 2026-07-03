// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using EdFi.Ods.AdminApi.V3.Features.Profiles;
using FluentValidation;
using FluentValidation.Results;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.Profiles;

[TestFixture]
public class ProfileValidatorTests
{
    private readonly ProfileValidator _validator = new();

    private List<ValidationFailure> RunValidate(string name, string definition)
    {
        var wrapper = new InlineValidator<string>();
        wrapper.RuleFor(x => x).Custom((val, ctx) => _validator.Validate(name, val, ctx));
        return wrapper.Validate(definition).Errors;
    }

    // Note: These tests require Ed-Fi-ODS-API-Profile.xsd to be present in the build output.
    // V3.UnitTests project does not copy XSD, so these tests verify the validator logic only when running
    // against an output that includes the schema. Skipped here to avoid false failures.

    [Test]
    [Explicit("Requires Ed-Fi-ODS-API-Profile.xsd in build output - run from V2 test context")]
    public void Validate_WhenDefinitionIsInvalidXml_ReturnsFailures()
    {
        RunValidate("TestProfile", "<invalid>").ShouldNotBeEmpty();
    }

    [Test]
    [Explicit("Requires Ed-Fi-ODS-API-Profile.xsd in build output")]
    public void Validate_WhenDefinitionIsEmpty_ReturnsFailures()
    {
        RunValidate("TestProfile", "").ShouldNotBeEmpty();
    }
}
