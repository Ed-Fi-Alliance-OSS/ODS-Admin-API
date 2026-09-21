// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.Audit;

[TestFixture]
public class DeletedEntitySnapshotRegistryTests
{
    [Test]
    public void Project_ForApiClient_ReturnsOnlyAllowlistedFields()
    {
        var registry = new DeletedEntitySnapshotRegistry();
        var apiClient = new ApiClient(true)
        {
            Name = "TestClient",
            IsApproved = true,
            KeyStatus = "Active"
        };

        var snapshot = registry.Project(apiClient);

        snapshot.ShouldNotBeNull();
        var json = JsonSerializer.Serialize(snapshot);
        json.ShouldContain("\"Key\":\"" + apiClient.Key + "\"");
        json.ShouldContain("\"Name\":\"TestClient\"");
        json.ShouldContain("\"IsApproved\":true");
        json.ShouldContain("\"KeyStatus\":\"Active\"");
        json.ShouldNotContain("Secret");
        json.ShouldNotContain(apiClient.Secret);
    }

    [Test]
    public void Project_ForUnregisteredType_ReturnsNull()
    {
        var registry = new DeletedEntitySnapshotRegistry();

        var snapshot = registry.Project(new object());

        snapshot.ShouldBeNull();
    }
}
