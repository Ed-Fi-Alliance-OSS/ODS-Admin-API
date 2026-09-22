// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Security.DataAccess.Models;
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
    public void Project_ForVendor_ReturnsOnlyAllowlistedFields()
    {
        var registry = new DeletedEntitySnapshotRegistry();
        var vendor = new Vendor { VendorName = "Acme Vendor" };

        var json = JsonSerializer.Serialize(registry.Project(vendor));

        json.ShouldContain("\"VendorName\":\"Acme Vendor\"");
        json.ShouldNotContain("VendorId");
        json.ShouldNotContain("Applications");
        json.ShouldNotContain("Users");
    }

    [Test]
    public void Project_ForApplication_ReturnsOnlyAllowlistedFieldsIncludingVendorName()
    {
        var registry = new DeletedEntitySnapshotRegistry();
        var application = new Application
        {
            ApplicationName = "App1",
            ClaimSetName = "CS1",
            OperationalContextUri = "uri",
            Vendor = new Vendor { VendorName = "Acme Vendor" }
        };

        var json = JsonSerializer.Serialize(registry.Project(application));

        json.ShouldContain("\"ApplicationName\":\"App1\"");
        json.ShouldContain("\"ClaimSetName\":\"CS1\"");
        json.ShouldContain("\"OperationalContextUri\":\"uri\"");
        json.ShouldContain("\"VendorName\":\"Acme Vendor\"");
        json.ShouldNotContain("ApplicationId");
        json.ShouldNotContain("ApiClients");
        json.ShouldNotContain("Profiles");
    }

    [Test]
    public void Project_ForOdsInstance_ExcludesConnectionString()
    {
        var registry = new DeletedEntitySnapshotRegistry();
        var odsInstance = new OdsInstance { Name = "ODS1", InstanceType = "Ods", ConnectionString = "super-secret-connection-string" };

        var json = JsonSerializer.Serialize(registry.Project(odsInstance));

        json.ShouldContain("\"Name\":\"ODS1\"");
        json.ShouldContain("\"InstanceType\":\"Ods\"");
        json.ShouldNotContain("ConnectionString");
        json.ShouldNotContain("super-secret-connection-string");
    }

    [Test]
    public void Project_ForOdsInstanceDerivative_IncludesParentNameAndExcludesBothConnectionStrings()
    {
        var registry = new DeletedEntitySnapshotRegistry();
        var derivative = new OdsInstanceDerivative
        {
            DerivativeType = "ReadReplica",
            ConnectionString = "derivative-secret",
            OdsInstance = new OdsInstance { Name = "ODS1", ConnectionString = "parent-secret" }
        };

        var json = JsonSerializer.Serialize(registry.Project(derivative));

        json.ShouldContain("\"DerivativeType\":\"ReadReplica\"");
        json.ShouldContain("\"OdsInstanceName\":\"ODS1\"");
        json.ShouldNotContain("ConnectionString");
        json.ShouldNotContain("derivative-secret");
        json.ShouldNotContain("parent-secret");
    }

    [Test]
    public void Project_ForOdsInstanceContext_IncludesParentNameAndExcludesParentConnectionString()
    {
        var registry = new DeletedEntitySnapshotRegistry();
        var context = new OdsInstanceContext
        {
            ContextKey = "Key1",
            ContextValue = "Value1",
            OdsInstance = new OdsInstance { Name = "ODS1", ConnectionString = "parent-secret" }
        };

        var json = JsonSerializer.Serialize(registry.Project(context));

        json.ShouldContain("\"ContextKey\":\"Key1\"");
        json.ShouldContain("\"ContextValue\":\"Value1\"");
        json.ShouldContain("\"OdsInstanceName\":\"ODS1\"");
        json.ShouldNotContain("ConnectionString");
        json.ShouldNotContain("parent-secret");
    }

    [Test]
    public void Project_ForProfile_ExcludesProfileDefinition()
    {
        var registry = new DeletedEntitySnapshotRegistry();
        var profile = new Profile { ProfileName = "P1", ProfileDefinition = "<Profile>sensitive-xml</Profile>" };

        var json = JsonSerializer.Serialize(registry.Project(profile));

        json.ShouldContain("\"ProfileName\":\"P1\"");
        json.ShouldNotContain("ProfileDefinition");
        json.ShouldNotContain("sensitive-xml");
    }

    [Test]
    public void Project_ForOdsInstanceManage_ExcludesDatabaseTemplateAndDatabaseName()
    {
        var registry = new DeletedEntitySnapshotRegistry();
        var manage = new OdsInstanceManage
        {
            Name = "Instance1",
            OdsInstanceName = "ODS1",
            Status = "Created",
            DatabaseTemplate = "template-secret",
            DatabaseName = "db-secret"
        };

        var json = JsonSerializer.Serialize(registry.Project(manage));

        json.ShouldContain("\"Name\":\"Instance1\"");
        json.ShouldContain("\"OdsInstanceName\":\"ODS1\"");
        json.ShouldContain("\"Status\":\"Created\"");
        json.ShouldNotContain("DatabaseTemplate");
        json.ShouldNotContain("DatabaseName");
        json.ShouldNotContain("template-secret");
        json.ShouldNotContain("db-secret");
    }

    [Test]
    public void Project_ForClaimSet_ReturnsOnlyAllowlistedFields()
    {
        var registry = new DeletedEntitySnapshotRegistry();
        var claimSet = new ClaimSet { ClaimSetName = "CS1", IsEdfiPreset = true, ForApplicationUseOnly = false };

        var json = JsonSerializer.Serialize(registry.Project(claimSet));

        json.ShouldContain("\"ClaimSetName\":\"CS1\"");
        json.ShouldContain("\"IsEdfiPreset\":true");
        json.ShouldContain("\"ForApplicationUseOnly\":false");
        json.ShouldNotContain("ClaimSetId");
    }

    [Test]
    public void Project_ForDeletedClaimSetResourceClaimAssociation_ReturnsAllFields()
    {
        var registry = new DeletedEntitySnapshotRegistry();
        var association = new DeletedClaimSetResourceClaimAssociation("CS1", "Resource1", new[] { 1, 2, 3 });

        var json = JsonSerializer.Serialize(registry.Project(association));

        json.ShouldContain("\"ClaimSetName\":\"CS1\"");
        json.ShouldContain("\"ResourceClaimName\":\"Resource1\"");
        json.ShouldContain("\"ActionIds\":[1,2,3]");
    }

    [Test]
    public void Project_ForUnregisteredType_ReturnsNull()
    {
        var registry = new DeletedEntitySnapshotRegistry();

        var snapshot = registry.Project(new object());

        snapshot.ShouldBeNull();
    }
}
