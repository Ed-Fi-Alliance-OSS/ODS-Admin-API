// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Ods.AdminApi.V3.Features.DataStores;
using EdFi.Ods.AdminApi.V3.Features.Tenants;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.Tenants;

[TestFixture]
public class TenantDetailModelTests
{
    [Test]
    public void DefaultConstructor_ShouldInitializePropertiesToEmpty()
    {
        // Act
        var tenantDetailModel = new TenantDetailModel() { TenantName = string.Empty };

        // Assert
        tenantDetailModel.TenantName.ShouldBe(string.Empty);
        tenantDetailModel.DataStores.ShouldBeEmpty();
    }

    [Test]
    public void Properties_ShouldBeSettable()
    {
        // Act
        var tenantName = "tenant 1";
        var educationOrganization = new EducationOrganizationModel()
        {
            EducationOrganizationId = 1001,
            NameOfInstitution = "name of institution 1",
            ShortNameOfInstitution = "short name of institution 1",
            Discriminator = "discriminator 1"
        };

        var odsInstance = new TenantDataStoreModel()
        {
            DataStoreId = 1,
            EducationOrganizations = [educationOrganization]
        };

        var tenantDetailModel = new TenantDetailModel()
        {
            TenantName = tenantName,
            DataStores = [odsInstance]
        };

        // Assert
        tenantDetailModel.TenantName.ShouldBe(tenantName);
        tenantDetailModel.DataStores.ShouldBe([odsInstance]);
        tenantDetailModel.DataStores[0].EducationOrganizations.ShouldBe([educationOrganization]);
    }

}


