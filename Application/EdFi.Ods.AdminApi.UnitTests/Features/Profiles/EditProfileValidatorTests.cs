// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Features.Profiles;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Profiles;

[TestFixture]
public class EditProfileValidatorTests
{
    private IGetProfilesQuery _getProfilesQuery = null!;
    private IGetProfileByIdQuery _getProfileByIdQuery = null!;
    private EditProfile.Validator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _getProfilesQuery = A.Fake<IGetProfilesQuery>();
        _getProfileByIdQuery = A.Fake<IGetProfileByIdQuery>();
        A.CallTo(() => _getProfilesQuery.Execute()).Returns(new List<Profile>());
        A.CallTo(() => _getProfileByIdQuery.Execute(A<int>._)).Returns(new Profile { ProfileId = 1, ProfileName = "Original" });
        _validator = new EditProfile.Validator(_getProfilesQuery, _getProfileByIdQuery);
    }

    [Test]
    public void Validate_WhenNameIsEmpty_ReturnsNameError()
    {
        var request = new EditProfile.EditProfileRequest { Id = 1, Name = string.Empty, Definition = string.Empty };

        var result = _validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.Name)).ShouldBeTrue();
    }

    [Test]
    public void Validate_WhenDefinitionIsEmpty_ReturnsDefinitionError()
    {
        var request = new EditProfile.EditProfileRequest { Id = 1, Name = "Original", Definition = string.Empty };

        var result = _validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.Definition)).ShouldBeTrue();
    }

    [Test]
    public void Validate_WhenChangedNameAlreadyExists_ReturnsNameError()
    {
        A.CallTo(() => _getProfilesQuery.Execute()).Returns(new List<Profile> { new() { ProfileName = "Existing" } });
        var request = new EditProfile.EditProfileRequest { Id = 1, Name = "Existing", Definition = string.Empty };

        var result = _validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.Name)).ShouldBeTrue();
    }
}
