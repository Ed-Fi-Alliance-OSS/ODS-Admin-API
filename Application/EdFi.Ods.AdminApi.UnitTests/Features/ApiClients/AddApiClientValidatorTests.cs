// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.Features.ApiClients;
using EdFi.Ods.AdminApi.Infrastructure.Commands;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.ApiClients
{
    [TestFixture]
    public class AddApiClientValidatorTests
    {
        private AddApiClient.Validator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new AddApiClient.Validator();
        }

        [Test]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var model = new AddApiClient.AddApiClientRequest { Name = "", ApplicationId = 1, OdsInstanceIds = new[] { 1 } };
            var result = _validator.Validate(model);
            result.Errors.Any(x => x.PropertyName == nameof(model.Name)).ShouldBeTrue();
        }

        [Test]
        public void Should_Have_Error_When_Name_Exceeds_Max_Length()
        {
            var model = new AddApiClient.AddApiClientRequest
            {
                Name = new string('A', ValidationConstants.MaximumApiClientNameLength + 1),
                ApplicationId = 1,
                OdsInstanceIds = new[] { 1 }
            };
            var result = _validator.Validate(model);
            result.Errors.Any(x => x.PropertyName == nameof(model.Name)).ShouldBeTrue();
        }

        [Test]
        public void Should_Have_Error_When_ApplicationId_Is_Zero()
        {
            var model = new AddApiClient.AddApiClientRequest { Name = "ValidName", ApplicationId = 0, OdsInstanceIds = new[] { 1 } };
            var result = _validator.Validate(model);
            result.Errors.Any(x => x.PropertyName == nameof(model.ApplicationId)).ShouldBeTrue();
        }

        [Test]
        public void Should_Have_Error_When_OdsInstanceIds_Is_Empty()
        {
            var model = new AddApiClient.AddApiClientRequest { Name = "ValidName", ApplicationId = 1, OdsInstanceIds = System.Array.Empty<int>() };
            var result = _validator.Validate(model);
            result.Errors.Any(x => x.PropertyName == nameof(model.OdsInstanceIds)).ShouldBeTrue();
        }

        [Test]
        public void Should_Not_Have_Error_For_Valid_Model()
        {
            var model = new AddApiClient.AddApiClientRequest
            {
                Name = "ValidName",
                ApplicationId = 1,
                OdsInstanceIds = new[] { 1 }
            };
            var result = _validator.Validate(model);
            result.IsValid.ShouldBeTrue();
        }
    }
}
