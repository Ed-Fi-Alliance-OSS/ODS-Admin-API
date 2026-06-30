// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features.DataStores;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using FluentValidation;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStores;

[TestFixture]
public class AddDataStoreValidatorTests
{
    [Test]
    public async Task Validator_WithDuplicateName_ThrowsValidationException()
    {
        var getDataStoresQuery = A.Fake<IGetDataStoresQuery>();
        A.CallTo(() => getDataStoresQuery.Execute()).Returns(
        [
            new OdsInstance { OdsInstanceId = 1, Name = "Existing", InstanceType = "Production" }
        ]);
        var validator = new AddDataStore.Validator(
            getDataStoresQuery,
            Options.Create(new AppSettings { DatabaseEngine = DatabaseEngineEnum.SqlServer }));
        var request = new AddDataStore.AddDataStoreRequest
        {
            Name = "Existing",
            DataStoreType = "Production",
            ConnectionString = "Server=(local);Database=Ods;Trusted_Connection=True;Encrypt=False"
        };

        var exception = await Should.ThrowAsync<ValidationException>(() => validator.GuardAsync(request));

        exception.Errors.ShouldContain(error => error.ErrorMessage == FeatureConstants.DataStoreAlreadyExistsMessage);
    }

    [Test]
    public async Task Validator_WithInvalidConnectionString_ThrowsValidationException()
    {
        var getDataStoresQuery = A.Fake<IGetDataStoresQuery>();
        A.CallTo(() => getDataStoresQuery.Execute()).Returns([]);
        var validator = new AddDataStore.Validator(
            getDataStoresQuery,
            Options.Create(new AppSettings { DatabaseEngine = DatabaseEngineEnum.SqlServer }));
        var request = new AddDataStore.AddDataStoreRequest
        {
            Name = "New",
            DataStoreType = "Production",
            ConnectionString = "not a connection string"
        };

        var exception = await Should.ThrowAsync<ValidationException>(() => validator.GuardAsync(request));

        exception.Errors.ShouldContain(error => error.ErrorMessage == FeatureConstants.DataStoreConnectionStringInvalid);
    }
}
