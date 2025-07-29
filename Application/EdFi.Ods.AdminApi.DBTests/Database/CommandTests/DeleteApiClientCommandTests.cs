// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.CommandTests;

[TestFixture]
internal class DeleteApiClientCommandTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldDeleteApiClient()
    {
        var apiClient = new ApiClient(true)
        {
            Name = "Test API Client",
            IsApproved = true,
            Application = new Application
            {
                ApplicationName = "Test Application",
                ClaimSetName = "FakeClaimSet",
                OperationalContextUri = "http://test.com",
                Profiles = null,
                Vendor = new Vendor
                {
                    VendorNamespacePrefixes = new List<VendorNamespacePrefix> { new VendorNamespacePrefix { NamespacePrefix = "http://tests.com" } },
                    VendorName = "Integration Tests"
                }
            }
        };
        Save(apiClient);

        Transaction(usersContext =>
        {
            var deleteApplicationCommand = new DeleteApiClientCommand(usersContext);
            deleteApplicationCommand.Execute(apiClient.ApiClientId);
        });

        Transaction(usersContext => usersContext.ApiClients.Where(a => a.ApiClientId == apiClient.ApiClientId).ToArray()).ShouldBeEmpty();
    }

    [Test]
    public void ShouldDeleteApiClientWithOdsInstances()
    {
        var apiClient = new ApiClient(true)
        {
            Name = "Test API Client",
            IsApproved = true,
            Application = new Application
            {
                ApplicationName = "Test Application",
                ClaimSetName = "FakeClaimSet",
                OperationalContextUri = "http://test.com",
                Profiles = null,
                Vendor = new Vendor
                {
                    VendorNamespacePrefixes = new List<VendorNamespacePrefix> { new VendorNamespacePrefix { NamespacePrefix = "http://tests.com" } },
                    VendorName = "Integration Tests"
                }
            }
        };

        var odsInstance = new OdsInstance
        {
            Name = "Test Instance",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };


        var apiClientOdsInstance = new ApiClientOdsInstance
        {
            OdsInstance = odsInstance,
            ApiClient = apiClient
        };


        Save(odsInstance, apiClient, apiClientOdsInstance);

        Transaction(usersContext =>
        {
            var deleteApplicationCommand = new DeleteApiClientCommand(usersContext);
            deleteApplicationCommand.Execute(apiClient.ApiClientId);
        });

        Transaction(usersContext => usersContext.ApiClients.Where(a => a.ApiClientId == apiClient.ApiClientId).ToArray()).ShouldBeEmpty();

        Transaction(usersContext => usersContext.ApiClientOdsInstances.Where(a => a.ApiClient.ApiClientId == apiClient.ApiClientId).ToArray()).ShouldBeEmpty();
    }
}
