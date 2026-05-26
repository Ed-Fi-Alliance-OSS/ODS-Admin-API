// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ClaimSets
{
    [TestFixture]
    public class AddClaimSetValidatorTests
    {
        private AddClaimSet.Validator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            var fakeGetAllClaimSetsQuery = A.Fake<IGetAllClaimSetsQuery>();
            A.CallTo(() => fakeGetAllClaimSetsQuery.Execute())
                .Returns(new List<ClaimSet>());

            _validator = new AddClaimSet.Validator(fakeGetAllClaimSetsQuery);
        }

        [TestCase("claimset name")]
        [TestCase("claimset\tname")]
        [TestCase("claimset\nname")]
        [TestCase(" leadingspace")]
        [TestCase("trailingspace ")]
        public void Should_Have_Error_When_Name_Contains_Whitespace(string name)
        {
            var request = new AddClaimSet.AddClaimSetRequest { Name = name };

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.Name)
                && x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
                .ShouldBeTrue();
        }

        [Test]
        public void Should_Not_Have_Error_When_Name_Has_No_Whitespace()
        {
            var request = new AddClaimSet.AddClaimSetRequest { Name = "claimsetname" };

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.PropertyName == nameof(request.Name)
                && x.ErrorMessage == FeatureConstants.ClaimSetNameNoWhitespaceMessage)
                .ShouldBeFalse();
        }
    }
}
