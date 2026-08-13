// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Features.ApiClients;
using EdFi.Ods.AdminApi.V3.Infrastructure.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ApiClients
{
    [TestFixture]
    public class AddApiClientValidatorTests
    {
        private SqlServerUsersContext _usersContext = null!;
        private AddApiClient.Validator _validator;

        [SetUp]
        public void SetUp()
        {
            _usersContext = new SqlServerUsersContext(
                new DbContextOptionsBuilder<SqlServerUsersContext>()
                    .UseInMemoryDatabase(databaseName: $"AddApiClientValidator_{Guid.NewGuid()}")
                    .Options);
            _validator = new AddApiClient.Validator(_usersContext);
        }

        [TearDown]
        public void TearDown()
        {
            _usersContext.Dispose();
        }

        [Test]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var model = new AddApiClient.AddApiClientRequest { Name = "", ApplicationId = 1, DataStoreIds = new[] { 1 } };
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
                DataStoreIds = new[] { 1 }
            };
            var result = _validator.Validate(model);
            result.Errors.Any(x => x.PropertyName == nameof(model.Name)).ShouldBeTrue();
        }

        [Test]
        public void Should_Have_Error_When_ApplicationId_Is_Zero()
        {
            var model = new AddApiClient.AddApiClientRequest { Name = "ValidName", ApplicationId = 0, DataStoreIds = new[] { 1 } };
            var result = _validator.Validate(model);
            result.Errors.Any(x => x.PropertyName == nameof(model.ApplicationId)).ShouldBeTrue();
        }

        [Test]
        public void Should_Have_Error_When_DataStoreIds_Is_Empty()
        {
            var model = new AddApiClient.AddApiClientRequest { Name = "ValidName", ApplicationId = 1, DataStoreIds = System.Array.Empty<int>() };
            var result = _validator.Validate(model);
            result.Errors.Any(x => x.PropertyName == nameof(model.DataStoreIds)).ShouldBeTrue();
        }

        [Test]
        public void Should_Have_Error_When_DataStoreIds_Is_Null()
        {
            var model = new AddApiClient.AddApiClientRequest { Name = "ValidName", ApplicationId = 1, DataStoreIds = null };
            var result = _validator.Validate(model);
            result.Errors.Any(x => x.PropertyName == nameof(model.DataStoreIds)).ShouldBeTrue();
        }

        [Test]
        public void Should_Not_Have_Error_For_Valid_Model()
        {
            var model = new AddApiClient.AddApiClientRequest
            {
                Name = "ValidName",
                ApplicationId = 1,
                DataStoreIds = new[] { 1 }
            };
            var result = _validator.Validate(model);
            result.IsValid.ShouldBeTrue();
        }

        [Test]
        public void Should_Have_Error_When_ApplicationId_And_Name_Already_Exist()
        {
            var vendor = new Vendor { VendorName = "Existing Vendor" };
            var application = new Application
            {
                ApplicationName = "Existing App",
                Vendor = vendor,
                OperationalContextUri = "uri"
            };
            _usersContext.Applications.Add(application);
            _usersContext.SaveChanges();
            _usersContext.ApiClients.Add(new ApiClient(true) { Name = "ValidName", Application = application });
            _usersContext.SaveChanges();

            var model = new AddApiClient.AddApiClientRequest
            {
                Name = "ValidName",
                ApplicationId = application.ApplicationId,
                DataStoreIds = new[] { 1 }
            };
            var result = _validator.Validate(model);

            result.Errors.Any(x => x.ErrorMessage == FeatureConstants.ApiClientCombinedKeyMustBeUnique).ShouldBeTrue();
        }
    }
}

