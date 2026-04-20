// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using NUnit.Framework;
using Shouldly;
using System.Linq;
using ClaimSet = EdFi.Security.DataAccess.Models.ClaimSet;

namespace EdFi.Ods.AdminApi.DBTests.V3.ClaimSetEditorTests;

[TestFixture]
public class AddClaimSetCommandTests : SecurityDataTestBase
{
    [Test]
    public void ShouldAddClaimSet()
    {
        var newClaimSet = new AddClaimSetModel { ClaimSetName = "TestClaimSet" };

        var addedClaimSetId = 0;
        ClaimSet addedClaimSet = null;
        using (var securityContext = TestContext)
        {
            var command = new AddClaimSetCommand(securityContext);
            addedClaimSetId = command.Execute(newClaimSet);
            addedClaimSet = securityContext.ClaimSets.Single(x => x.ClaimSetId == addedClaimSetId);
        }
        addedClaimSet.ClaimSetName.ShouldBe(newClaimSet.ClaimSetName);
        addedClaimSet.ForApplicationUseOnly.ShouldBe(false);
        addedClaimSet.IsEdfiPreset.ShouldBe(false);
    }
}


