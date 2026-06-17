// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.V3.Features.Vendors;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.Vendors
{
    [TestFixture]
    public class EditVendorValidatorTests
    {
        private EditVendor.Validator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _validator = new EditVendor.Validator();
        }

        [Test]
        public void Should_Have_Error_When_Id_Is_Zero()
        {
            var request = ValidRequest();
            request.Id = 0;

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.Id)).ShouldBeTrue();
        }

        [Test]
        public void Should_Have_Error_When_Company_Is_Empty()
        {
            var request = ValidRequest();
            request.Company = string.Empty;

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.Company)).ShouldBeTrue();
        }

        [Test]
        public void Should_Have_Error_When_Company_Is_Reserved()
        {
            var request = ValidRequest();
            request.Company = VendorExtensions.ReservedNames[0];

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.Company)).ShouldBeTrue();
        }

        [Test]
        public void Should_Have_Error_When_ContactName_Is_Empty()
        {
            var request = ValidRequest();
            request.ContactName = string.Empty;

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.ContactName)).ShouldBeTrue();
        }

        [Test]
        public void Should_Have_Error_When_ContactEmailAddress_Is_Invalid()
        {
            var request = ValidRequest();
            request.ContactEmailAddress = "invalid-email";

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.ContactEmailAddress)).ShouldBeTrue();
        }

        [Test]
        public void Should_Not_Have_Error_For_Valid_Request()
        {
            var request = ValidRequest();

            var result = _validator.Validate(request);

            result.IsValid.ShouldBeTrue();
        }

        private static EditVendor.EditVendorRequest ValidRequest()
        {
            return new EditVendor.EditVendorRequest
            {
                Id = 1,
                Company = "Acme Vendor",
                NamespacePrefixes = "https://acme.org/ns",
                ContactName = "Alice",
                ContactEmailAddress = "alice@acme.org"
            };
        }
    }
}


