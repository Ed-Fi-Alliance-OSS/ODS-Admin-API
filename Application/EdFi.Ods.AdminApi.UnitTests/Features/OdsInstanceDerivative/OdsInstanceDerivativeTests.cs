// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.OdsInstanceDerivative;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using FluentValidation;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using DbOdsInstanceDerivative = EdFi.Admin.DataAccess.Models.OdsInstanceDerivative;

namespace EdFi.Ods.AdminApi.UnitTests.Features.OdsInstanceDerivative;

[TestFixture]
public class OdsInstanceDerivativeTests
{
    private IGetOdsInstanceQuery _getOdsInstanceQuery = null!;
    private IGetOdsInstanceDerivativesQuery _getOdsInstanceDerivativesQuery = null!;
    private IOptions<AppSettings> _options = null!;

    [SetUp]
    public void SetUp()
    {
        _getOdsInstanceQuery = A.Fake<IGetOdsInstanceQuery>();
        _getOdsInstanceDerivativesQuery = A.Fake<IGetOdsInstanceDerivativesQuery>();
        _options = Options.Create(new AppSettings { DatabaseEngine = DatabaseEngineEnum.SqlServer });
        A.CallTo(() => _getOdsInstanceQuery.Execute(A<int>._)).Returns(new OdsInstance { OdsInstanceId = 10 });
        A.CallTo(() => _getOdsInstanceDerivativesQuery.Execute()).Returns([]);
    }

    [Test]
    public async Task AddValidator_WithInvalidDerivativeType_ThrowsValidationException()
    {
        var validator = new AddOdsInstanceDerivative.Validator(_getOdsInstanceQuery, _getOdsInstanceDerivativesQuery, _options);
        var request = new AddOdsInstanceDerivative.AddOdsInstanceDerivativeRequest
        {
            OdsInstanceId = 10,
            DerivativeType = "Mirror",
            ConnectionString = "Server=(local);Database=Ods;Trusted_Connection=True;Encrypt=False"
        };

        var exception = await Should.ThrowAsync<ValidationException>(() => validator.GuardAsync(request));

        exception.Errors.ShouldContain(error => error.ErrorMessage == FeatureConstants.OdsInstanceDerivativeDerivativeTypeNotValid);
    }

    [Test]
    public async Task EditValidator_WithSameDerivativeRecord_AllowsUnchangedCombinedKey()
    {
        A.CallTo(() => _getOdsInstanceDerivativesQuery.Execute()).Returns(
        [
            new DbOdsInstanceDerivative
            {
                OdsInstanceDerivativeId = 3,
                DerivativeType = "Snapshot",
                ConnectionString = "Server=(local);Database=Ods;Trusted_Connection=True;Encrypt=False",
                OdsInstance = new OdsInstance { OdsInstanceId = 10 }
            }
        ]);
        var validator = new EditOdsInstanceDerivative.Validator(_getOdsInstanceQuery, _getOdsInstanceDerivativesQuery, _options);
        var request = new EditOdsInstanceDerivative.EditOdsInstanceDerivativeRequest
        {
            Id = 3,
            OdsInstanceId = 10,
            DerivativeType = "snapshot",
            ConnectionString = "Server=(local);Database=Ods;Trusted_Connection=True;Encrypt=False"
        };

        await Should.NotThrowAsync(() => validator.GuardAsync(request));
    }

    [Test]
    public void ToModel_MapsDerivativeFieldsAndParentOdsInstanceId()
    {
        var source = new DbOdsInstanceDerivative
        {
            OdsInstanceDerivativeId = 22,
            DerivativeType = "ReadReplica",
            ConnectionString = "encrypted",
            OdsInstance = new OdsInstance { OdsInstanceId = 99 }
        };

        var model = OdsInstanceDerivativeMapper.ToModel(source);

        model.Id.ShouldBe(22);
        model.OdsInstanceId.ShouldBe(99);
        model.DerivativeType.ShouldBe("ReadReplica");
    }

    [Test]
    public void ToModelList_MapsAllDerivatives()
    {
        var source = new List<DbOdsInstanceDerivative>
        {
            new() { OdsInstanceDerivativeId = 1, DerivativeType = "Snapshot" },
            new() { OdsInstanceDerivativeId = 2, DerivativeType = "ReadReplica" }
        };

        var models = OdsInstanceDerivativeMapper.ToModelList(source);

        models.Select(x => x.Id).ShouldBe([1, 2]);
    }
}
