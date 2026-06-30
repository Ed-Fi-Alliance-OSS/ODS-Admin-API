// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Features.OdsInstances;
using NUnit.Framework;
using Shouldly;
using DbOdsInstanceContext = EdFi.Admin.DataAccess.Models.OdsInstanceContext;
using DbOdsInstanceDerivative = EdFi.Admin.DataAccess.Models.OdsInstanceDerivative;

namespace EdFi.Ods.AdminApi.UnitTests.Features.OdsInstances;

[TestFixture]
public class OdsInstanceMapperTests
{
    [Test]
    public void ToModel_MapsSummaryFields()
    {
        var source = new OdsInstance
        {
            OdsInstanceId = 17,
            Name = "Grand Bend",
            InstanceType = "Production"
        };

        var model = OdsInstanceMapper.ToModel(source);

        model.OdsInstanceId.ShouldBe(17);
        model.Name.ShouldBe("Grand Bend");
        model.InstanceType.ShouldBe("Production");
    }

    [Test]
    public void ToDetailModel_MapsNestedContextsAndDerivatives()
    {
        var source = new OdsInstance
        {
            OdsInstanceId = 21,
            Name = "Sample",
            InstanceType = "Sandbox",
            OdsInstanceContexts =
            [
                new DbOdsInstanceContext
                {
                    OdsInstanceContextId = 31,
                    ContextKey = "SchoolYear",
                    ContextValue = "2026"
                }
            ],
            OdsInstanceDerivatives =
            [
                new DbOdsInstanceDerivative
                {
                    OdsInstanceDerivativeId = 41,
                    DerivativeType = "ReadReplica"
                }
            ]
        };

        source.OdsInstanceContexts.First().OdsInstance = source;
        source.OdsInstanceDerivatives.First().OdsInstance = source;

        var model = OdsInstanceMapper.ToDetailModel(source);

        model.OdsInstanceId.ShouldBe(21);
        model.OdsInstanceContexts!.Single().OdsInstanceId.ShouldBe(21);
        model.OdsInstanceContexts!.Single().ContextKey.ShouldBe("SchoolYear");
        model.OdsInstanceDerivatives!.Single().OdsInstanceId.ShouldBe(21);
        model.OdsInstanceDerivatives!.Single().DerivativeType.ShouldBe("ReadReplica");
    }

    [Test]
    public void ToModelList_MapsAllItemsInOrder()
    {
        var source = new[]
        {
            new OdsInstance { OdsInstanceId = 1, Name = "A", InstanceType = "TypeA" },
            new OdsInstance { OdsInstanceId = 2, Name = "B", InstanceType = "TypeB" }
        };

        var models = OdsInstanceMapper.ToModelList(source);

        models.Select(x => x.OdsInstanceId).ShouldBe([1, 2]);
        models.Select(x => x.Name).ShouldBe(["A", "B"]);
    }
}
