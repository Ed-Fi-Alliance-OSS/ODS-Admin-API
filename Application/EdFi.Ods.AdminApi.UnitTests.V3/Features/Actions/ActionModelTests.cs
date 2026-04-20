// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Features.Actions;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.V3.Features.Actions;

[TestFixture]
public class ActionModelTests
{
    [Test]
    public void ActionModel_DefaultValues_AreSetCorrectly()
    {
        var model = new ActionModel();

        model.Id.ShouldBe(0);
        model.Name.ShouldBeNull();
        model.Uri.ShouldBeNull();
    }

    [Test]
    public void ActionModel_SetProperties_ValuesAreSetCorrectly()
    {
        var model = new ActionModel
        {
            Id = 10,
            Name = "Read",
            Uri = "/resource/read"
        };

        model.Id.ShouldBe(10);
        model.Name.ShouldBe("Read");
        model.Uri.ShouldBe("/resource/read");
    }
}


