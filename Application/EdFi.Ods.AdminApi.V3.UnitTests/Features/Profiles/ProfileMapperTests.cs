// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Features.Profiles;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.Profiles;

[TestFixture]
public class ProfileMapperTests
{
    [Test]
    public void ToModel_MapsProfileSummaryFields()
    {
        var source = new Profile { ProfileId = 42, ProfileName = "Assessment" };

        var model = ProfileMapper.ToModel(source);

        model.Id.ShouldBe(42);
        model.Name.ShouldBe("Assessment");
    }

    [Test]
    public void ToDetailsModel_MapsProfileDefinition()
    {
        var source = new Profile { ProfileId = 42, ProfileName = "Assessment", ProfileDefinition = "<Profile />" };

        var model = ProfileMapper.ToDetailsModel(source);

        model.Id.ShouldBe(42);
        model.Name.ShouldBe("Assessment");
        model.Definition.ShouldBe("<Profile />");
    }

    [Test]
    public void ToModelList_PreservesSourceOrder()
    {
        var source = new List<Profile>
        {
            new() { ProfileId = 1, ProfileName = "One" },
            new() { ProfileId = 2, ProfileName = "Two" }
        };

        var models = ProfileMapper.ToModelList(source);

        models.Count.ShouldBe(2);
        models[0].Id.ShouldBe(1);
        models[1].Id.ShouldBe(2);
    }
}
