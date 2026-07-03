// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using EdFi.Ods.AdminApi.Features.Profiles;
using FluentValidation;
using FluentValidation.Results;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Profiles;

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

    [Test]
    public void Validate_WhenDefinitionIsInvalidXml_ReturnsFailures()
    {
        RunValidate("TestProfile", "<invalid>").ShouldNotBeEmpty();
    }

    [Test]
    public void Validate_WhenProfileNameDoesNotMatchDefinition_ReturnsFailures()
    {
        const string definition = @"<?xml version=""1.0"" encoding=""utf-8""?><Profile name=""WrongName""></Profile>";
        RunValidate("TestProfile", definition).Count.ShouldBeGreaterThan(0);
    }

    [Test]
    public void Validate_WhenDefinitionIsEmpty_ReturnsFailures()
    {
        RunValidate("TestProfile", "").ShouldNotBeEmpty();
    }
}
