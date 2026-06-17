// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Helpers;

[TestFixture]
public class ConfigConnectionStringsProviderTests
{
    [Test]
    public void GetConnectionString_WhenTenantContextProvidesOverrides_ReturnsTenantSpecificConnectionString()
    {
        var configuration = CreateConfiguration();
        var tenantContextProvider = A.Fake<IContextProvider<TenantConfiguration>>();

        A.CallTo(() => tenantContextProvider.Get()).Returns(new TenantConfiguration
        {
            TenantIdentifier = "tenant1",
            AdminConnectionString = "Host=tenant-admin;Database=EdFi_Admin;",
            SecurityConnectionString = "Host=tenant-security;Database=EdFi_Security;",
            OdsConnectionString = "Host=tenant-ods;Database=EdFi_Ods;",
            MasterConnectionString = "Host=tenant-master;Database=postgres;"
        });

        var sut = new ConfigConnectionStringsProvider(
            configuration,
            tenantContextProvider,
            Options.Create(new AppSettings { MultiTenancy = true }));

        sut.GetConnectionString("EdFi_Ods").ShouldBe("Host=tenant-ods;Database=EdFi_Ods;");
        sut.GetConnectionString("EdFi_Master").ShouldBe("Host=tenant-master;Database=postgres;");
    }

    [Test]
    public void GetConnectionString_WhenTenantContextIsMissing_FallsBackToTopLevelConnectionString()
    {
        var configuration = CreateConfiguration();
        var tenantContextProvider = A.Fake<IContextProvider<TenantConfiguration>>();
        A.CallTo(() => tenantContextProvider.Get()).Returns((TenantConfiguration)null);

        var sut = new ConfigConnectionStringsProvider(
            configuration,
            tenantContextProvider,
            Options.Create(new AppSettings { MultiTenancy = true }));

        sut.GetConnectionString("EdFi_Ods").ShouldBe("Host=default-ods;Database=EdFi_Ods;");
        sut.GetConnectionString("EdFi_Master").ShouldBe("Host=default-master;Database=postgres;");
    }

    private static IConfiguration CreateConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:EdFi_Admin"] = "Host=default-admin;Database=EdFi_Admin;",
                ["ConnectionStrings:EdFi_Security"] = "Host=default-security;Database=EdFi_Security;",
                ["ConnectionStrings:EdFi_Ods"] = "Host=default-ods;Database=EdFi_Ods;",
                ["ConnectionStrings:EdFi_Master"] = "Host=default-master;Database=postgres;"
            })
            .Build();
}
