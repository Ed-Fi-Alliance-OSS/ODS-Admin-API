// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.V2.Features.Vendors;
using EdFi.Ods.AdminApi.V2.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.V2.Features.Vendors
{
    [TestFixture]
    public class AddVendorValidatorTests
    {
        private AddVendor.Validator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _validator = new AddVendor.Validator();
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
        public void Should_Have_Error_When_NamespacePrefix_Exceeds_MaxLength()
        {
            var request = ValidRequest();
            request.NamespacePrefixes = new string('x', 256);

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.NamespacePrefixes)).ShouldBeTrue();
        }

        [Test]
        public void Should_Not_Have_Error_For_Valid_Request()
        {
            var request = ValidRequest();

            var result = _validator.Validate(request);

            result.IsValid.ShouldBeTrue();
        }

        private static AddVendor.AddVendorRequest ValidRequest()
        {
            return new AddVendor.AddVendorRequest
            {
                Company = "Acme Vendor",
                NamespacePrefixes = "https://acme.org/ns",
                ContactName = "Alice",
                ContactEmailAddress = "alice@acme.org"
            };
        }
    }
}
