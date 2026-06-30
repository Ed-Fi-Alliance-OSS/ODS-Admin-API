// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FakeItEasy;
using FluentValidation;
using NUnit.Framework;
using Shouldly;
using DbOdsInstanceContext = EdFi.Admin.DataAccess.Models.OdsInstanceContext;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.DataStoreContexts;

[TestFixture]
public class DataStoreContextTests
{
    private IGetDataStoreQuery _getDataStoreQuery = null!;
    private IGetDataStoreContextsQuery _getDataStoreContextsQuery = null!;

    [SetUp]
    public void SetUp()
    {
        _getDataStoreQuery = A.Fake<IGetDataStoreQuery>();
        _getDataStoreContextsQuery = A.Fake<IGetDataStoreContextsQuery>();
        A.CallTo(() => _getDataStoreQuery.Execute(A<int>._)).Returns(new OdsInstance { OdsInstanceId = 10 });
        A.CallTo(() => _getDataStoreContextsQuery.Execute()).Returns([]);
    }

    [Test]
    public async Task AddValidator_WithDuplicateContextKeyForSameDataStore_ThrowsValidationException()
    {
        A.CallTo(() => _getDataStoreContextsQuery.Execute()).Returns(
        [
            new DbOdsInstanceContext
            {
                OdsInstanceContextId = 1,
                ContextKey = "SchoolYear",
                ContextValue = "2026",
                OdsInstance = new OdsInstance { OdsInstanceId = 10 }
            }
        ]);
        var validator = new AddDataStoreContext.Validator(_getDataStoreQuery, _getDataStoreContextsQuery);
        var request = new AddDataStoreContext.AddDataStoreContextRequest
        {
            DataStoreId = 10,
            ContextKey = "schoolyear",
            ContextValue = "2027"
        };

        var exception = await Should.ThrowAsync<ValidationException>(() => validator.GuardAsync(request));

        exception.Errors.ShouldContain(error => error.ErrorMessage == FeatureConstants.DataStoreContextCombinedKeyMustBeUnique);
    }

    [Test]
    public void ToModel_MapsContextFieldsAndParentDataStoreId()
    {
        var source = new DbOdsInstanceContext
        {
            OdsInstanceContextId = 12,
            ContextKey = "SchoolYear",
            ContextValue = "2026",
            OdsInstance = new OdsInstance { OdsInstanceId = 99 }
        };

        var model = DataStoreContextMapper.ToModel(source);

        model.DataStoreContextId.ShouldBe(12);
        model.DataStoreId.ShouldBe(99);
        model.ContextKey.ShouldBe("SchoolYear");
        model.ContextValue.ShouldBe("2026");
    }

    [Test]
    public void ToModelList_MapsAllContexts()
    {
        var source = new List<DbOdsInstanceContext>
        {
            new() { OdsInstanceContextId = 1, ContextKey = "A", ContextValue = "1" },
            new() { OdsInstanceContextId = 2, ContextKey = "B", ContextValue = "2" }
        };

        var models = DataStoreContextMapper.ToModelList(source);

        models.Select(x => x.DataStoreContextId).ShouldBe([1, 2]);
    }
}
