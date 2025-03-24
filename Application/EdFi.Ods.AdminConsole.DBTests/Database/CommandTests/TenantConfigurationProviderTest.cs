// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;

namespace EdFi.Ods.AdminConsole.DBTests.Database.CommandTests;

public class TenantConfigurationProviderTest : ITenantConfigurationProvider
{
    public IDictionary<string, TenantConfiguration> Get()
    {
        return new Dictionary<string, TenantConfiguration>
        {
            {
                "tenant1",
                new TenantConfiguration()
                {
                    AdminConnectionString = "host=localhost;port=5432;username=postgres;password=admin;database=EdFi_Admin;pooling=false",
                    SecurityConnectionString = "host=localhost;port=5432;username=postgres;password=admin;database=EdFi_Security;pooling=false",
                    TenantIdentifier = "tenant1"
                }
            }
        };
    }
}
