// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Features.Applications;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.Applications
{
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
        public void Should_Have_Error_When_ClaimSetName_Contains_Whitespace()
        {
            var request = ValidRequest();
            request.ClaimSetName = "claimset name";

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.ClaimSetName)
                && x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
                .ShouldBeTrue();
        }

        [Test]
        public void Should_Not_Have_Error_When_ClaimSetName_Has_No_Whitespace()
        {
            var request = ValidRequest();
            request.ClaimSetName = "claimsetname";

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.ClaimSetName)
                && x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
                .ShouldBeFalse();
        }

        private static EditApplication.EditApplicationRequest ValidRequest()
        {
            return new EditApplication.EditApplicationRequest
            {
                Id = 1,
                ApplicationName = "Test Application",
                VendorId = 1,
                ClaimSetName = "TestClaimSet",
                EducationOrganizationIds = new long[] { 1L },
                DataStoreIds = new[] { 1 }
            };
        }
    }
}
