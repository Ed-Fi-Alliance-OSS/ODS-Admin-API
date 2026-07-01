// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using ActionEnumeration = EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor.Action;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure;

[TestFixture]
public class EnumerationTests
{
    [Test]
    public void GetAll_WhenClaimSetEditorActionsExist_ReturnsDeclaredActions()
    {
        var actions = ActionEnumeration.GetAll();

        actions.ShouldBe(
            [
                ActionEnumeration.Create,
                ActionEnumeration.Read,
                ActionEnumeration.Update,
                ActionEnumeration.Delete
            ]
        );
    }

    [Test]
    public void FromValue_WhenValueMatchesAction_ReturnsMatchingAction()
    {
        var action = ActionEnumeration.FromValue("Read");

        action.ShouldBe(ActionEnumeration.Read);
    }

    [Test]
    public void TryParse_WhenDisplayNameDoesNotMatch_ReturnsFalseAndNullResult()
    {
        var parsed = ActionEnumeration.TryParse("Unknown", out var action);

        parsed.ShouldBeFalse();
        action.ShouldBeNull();
    }

    [Test]
    public void Parse_WhenDisplayNameDoesNotMatch_ThrowsArgumentExceptionForValue()
    {
        var exception = Should.Throw<ArgumentException>(() => ActionEnumeration.Parse("Unknown"));

        exception.ParamName.ShouldBe("value");
        exception.Message.ShouldContain("'Unknown' is not a valid display name");
    }

    [Test]
    public void EqualityOperators_WhenActionsHaveSameValue_ReturnTrueForEqualAndFalseForNotEqual()
    {
        var action = ActionEnumeration.FromValue("Create");

        (action == ActionEnumeration.Create).ShouldBeTrue();
        (action != ActionEnumeration.Create).ShouldBeFalse();
    }
}
