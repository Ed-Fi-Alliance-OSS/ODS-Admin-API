// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Features.Applications;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.Applications
{
    [TestFixture]
    public class AddApplicationValidatorTests
    {
        private SqlServerUsersContext _usersContext = null!;
        private AddApplication.Validator _validator = null!;

        [SetUp]
        public void SetUp()
        {
            _usersContext = new SqlServerUsersContext(
                new DbContextOptionsBuilder<SqlServerUsersContext>()
                    .UseInMemoryDatabase(databaseName: $"AddApplicationValidator_{Guid.NewGuid()}")
                    .Options);
            _validator = new AddApplication.Validator(_usersContext);
        }

        [TearDown]
        public void TearDown()
        {
            _usersContext.Dispose();
        }

        [TestCase("claimset name")]
        [TestCase("claimset\tname")]
        [TestCase("claimset\nname")]
        [TestCase(" leadingspace")]
        [TestCase("trailingspace ")]
        public void Should_Have_Error_When_ClaimSetName_Contains_Whitespace(string claimSetName)
        {
            var request = ValidRequest();
            request.ClaimSetName = claimSetName;

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

        [Test]
        public void Should_Have_Error_When_VendorId_And_ApplicationName_Already_Exist()
        {
            var request = ValidRequest();
            var vendor = new Vendor { VendorId = request.VendorId, VendorName = "Existing Vendor" };
            _usersContext.Vendors.Add(vendor);
            _usersContext.Applications.Add(new Application
            {
                ApplicationName = request.ApplicationName,
                Vendor = vendor,
                OperationalContextUri = "uri"
            });
            _usersContext.SaveChanges();

            var result = _validator.Validate(request);

            result.Errors.Any(x => x.ErrorMessage == FeatureConstants.ApplicationCombinedKeyMustBeUnique)
                .ShouldBeTrue();
        }

        private static AddApplication.AddApplicationRequest ValidRequest()
        {
            return new AddApplication.AddApplicationRequest
            {
                ApplicationName = "Test Application",
                VendorId = 1,
                ClaimSetName = "TestClaimSet",
                EducationOrganizationIds = new long[] { 1L },
                DataStoreIds = new[] { 1 }
            };
        }
    }
}
