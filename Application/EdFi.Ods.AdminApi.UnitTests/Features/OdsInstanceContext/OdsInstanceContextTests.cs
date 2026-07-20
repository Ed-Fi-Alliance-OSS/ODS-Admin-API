// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Features.OdsInstanceContext;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FakeItEasy;
using FluentValidation;
using NUnit.Framework;
using Shouldly;
using DbOdsInstanceContext = EdFi.Admin.DataAccess.Models.OdsInstanceContext;

namespace EdFi.Ods.AdminApi.UnitTests.Features.OdsInstanceContext;

[TestFixture]
public class OdsInstanceContextTests
{
    private IGetOdsInstanceQuery _getOdsInstanceQuery = null!;
    private IGetOdsInstanceContextsQuery _getOdsInstanceContextsQuery = null!;

    [SetUp]
    public void SetUp()
    {
        _getOdsInstanceQuery = A.Fake<IGetOdsInstanceQuery>();
        _getOdsInstanceContextsQuery = A.Fake<IGetOdsInstanceContextsQuery>();
        A.CallTo(() => _getOdsInstanceQuery.Execute(A<int>._)).Returns(new OdsInstance { OdsInstanceId = 10 });
        A.CallTo(() => _getOdsInstanceContextsQuery.Execute()).Returns([]);
    }

    [Test]
    public async Task AddValidator_WithDuplicateContextKeyForSameOdsInstance_ThrowsValidationException()
    {
        A.CallTo(() => _getOdsInstanceContextsQuery.Execute()).Returns(
        [
            new DbOdsInstanceContext
            {
                OdsInstanceContextId = 1,
                ContextKey = "SchoolYear",
                ContextValue = "2026",
                OdsInstance = new OdsInstance { OdsInstanceId = 10 }
            }
        ]);
        var validator = new AddOdsInstanceContext.Validator(_getOdsInstanceQuery, _getOdsInstanceContextsQuery);
        var request = new AddOdsInstanceContext.AddOdsInstanceContextRequest
        {
            OdsInstanceId = 10,
            ContextKey = "schoolyear",
            ContextValue = "2027"
        };

        var exception = await Should.ThrowAsync<ValidationException>(() => validator.GuardAsync(request));

        exception.Errors.ShouldContain(error => error.ErrorMessage == FeatureConstants.OdsInstanceContextCombinedKeyMustBeUnique);
    }

    [Test]
    public async Task EditValidator_WithSameContextRecord_AllowsUnchangedCombinedKey()
    {
        A.CallTo(() => _getOdsInstanceContextsQuery.Execute()).Returns(
        [
            new DbOdsInstanceContext
            {
                OdsInstanceContextId = 4,
                ContextKey = "SchoolYear",
                ContextValue = "2026",
                OdsInstance = new OdsInstance { OdsInstanceId = 10 }
            }
        ]);
        var validator = new EditOdsInstanceContext.Validator(_getOdsInstanceQuery, _getOdsInstanceContextsQuery);
        var request = new EditOdsInstanceContext.EditOdsInstanceContextRequest
        {
            Id = 4,
            OdsInstanceId = 10,
            ContextKey = "schoolyear",
            ContextValue = "2027"
        };

        await Should.NotThrowAsync(() => validator.GuardAsync(request));
    }

    [Test]
    public void ToModel_MapsContextFieldsAndParentOdsInstanceId()
    {
        var source = new DbOdsInstanceContext
        {
            OdsInstanceContextId = 12,
            ContextKey = "SchoolYear",
            ContextValue = "2026",
            OdsInstance = new OdsInstance { OdsInstanceId = 99 }
        };

        var model = OdsInstanceContextMapper.ToModel(source);

        model.OdsInstanceContextId.ShouldBe(12);
        model.OdsInstanceId.ShouldBe(99);
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

        var models = OdsInstanceContextMapper.ToModelList(source);

        models.Select(x => x.OdsInstanceContextId).ShouldBe([1, 2]);
    }
}
