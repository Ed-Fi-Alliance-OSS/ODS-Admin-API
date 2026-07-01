// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using FluentValidation;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using DbOdsInstanceDerivative = EdFi.Admin.DataAccess.Models.OdsInstanceDerivative;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStoreDerivatives;

[TestFixture]
public class DataStoreDerivativeTests
{
    private IGetDataStoreQuery _getDataStoreQuery = null!;
    private IGetDataStoreDerivativesQuery _getDataStoreDerivativesQuery = null!;
    private IOptions<AppSettings> _options = null!;

    [SetUp]
    public void SetUp()
    {
        _getDataStoreQuery = A.Fake<IGetDataStoreQuery>();
        _getDataStoreDerivativesQuery = A.Fake<IGetDataStoreDerivativesQuery>();
        _options = Options.Create(new AppSettings { DatabaseEngine = DatabaseEngineEnum.SqlServer });
        A.CallTo(() => _getDataStoreQuery.Execute(A<int>._)).Returns(new OdsInstance { OdsInstanceId = 10 });
        A.CallTo(() => _getDataStoreDerivativesQuery.Execute()).Returns([]);
    }

    [Test]
    public async Task AddValidator_WithInvalidDerivativeType_ThrowsValidationException()
    {
        var validator = new AddDataStoreDerivative.Validator(_getDataStoreQuery, _getDataStoreDerivativesQuery, _options);
        var request = new AddDataStoreDerivative.AddDataStoreDerivativeRequest
        {
            DataStoreId = 10,
            DerivativeType = "Mirror",
            ConnectionString = "Server=(local);Database=Ods;Trusted_Connection=True;Encrypt=False"
        };

        var exception = await Should.ThrowAsync<ValidationException>(() => validator.GuardAsync(request));

        exception.Errors.ShouldContain(error => error.ErrorMessage == FeatureConstants.DataStoreDerivativeTypeNotValid);
    }

    [Test]
    public void ToModel_MapsDerivativeFieldsAndParentDataStoreId()
    {
        var source = new DbOdsInstanceDerivative
        {
            OdsInstanceDerivativeId = 22,
            DerivativeType = "ReadReplica",
            ConnectionString = "encrypted",
            OdsInstance = new OdsInstance { OdsInstanceId = 99 }
        };

        var model = DataStoreDerivativeMapper.ToModel(source);

        model.DataStoreDerivativeId.ShouldBe(22);
        model.DataStoreId.ShouldBe(99);
        model.DerivativeType.ShouldBe("ReadReplica");
        model.ConnectionString.ShouldBe("encrypted");
    }

    [Test]
    public void ToModelList_MapsAllDerivatives()
    {
        var source = new List<DbOdsInstanceDerivative>
        {
            new() { OdsInstanceDerivativeId = 1, DerivativeType = "Snapshot" },
            new() { OdsInstanceDerivativeId = 2, DerivativeType = "ReadReplica" }
        };

        var models = DataStoreDerivativeMapper.ToModelList(source);

        models.Select(x => x.DataStoreDerivativeId).ShouldBe([1, 2]);
    }
}
