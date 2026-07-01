// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Features.Profiles;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.Profiles;

[TestFixture]
public class AddProfileValidatorTests
{
    private IGetProfilesQuery _getProfilesQuery = null!;
    private AddProfile.Validator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _getProfilesQuery = A.Fake<IGetProfilesQuery>();
        A.CallTo(() => _getProfilesQuery.Execute()).Returns(new List<Profile>());
        _validator = new AddProfile.Validator(_getProfilesQuery);
    }

    [Test]
    public void Validate_WhenNameIsEmpty_ReturnsNameError()
    {
        var request = new AddProfile.AddProfileRequest { Name = string.Empty, Definition = string.Empty };

        var result = _validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.Name)).ShouldBeTrue();
    }

    [Test]
    public void Validate_WhenDefinitionIsEmpty_ReturnsDefinitionError()
    {
        var request = new AddProfile.AddProfileRequest { Name = "Assessment", Definition = string.Empty };

        var result = _validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.Definition)).ShouldBeTrue();
    }

    [Test]
    public void Validate_WhenProfileNameAlreadyExists_ReturnsNameError()
    {
        A.CallTo(() => _getProfilesQuery.Execute()).Returns(new List<Profile> { new() { ProfileName = "Assessment" } });
        var request = new AddProfile.AddProfileRequest { Name = "Assessment", Definition = string.Empty };

        var result = _validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.Name)).ShouldBeTrue();
    }
}
