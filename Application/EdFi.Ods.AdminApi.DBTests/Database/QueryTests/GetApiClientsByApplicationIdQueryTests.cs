// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using VendorUser = EdFi.Admin.DataAccess.Models.User;

namespace EdFi.Ods.AdminApi.DBTests.Database.QueryTests;

[TestFixture]
public class GetApiClientsByApplicationIdQueryTests : PlatformUsersContextTestBase
{
    private IOptions<AppSettings> _options { get; set; }
    private int applicationId = 0;

    [SetUp]
    public virtual async Task FixtureSetup()
    {
        _options = Testing.GetAppSettings();
        _options.Value.PreventDuplicateApplications = false;
        LoadApiClients();
        await Task.Yield();
    }

    [Test]
    public void ShouldGetAllApliClientsPerApplication()
    {
        IReadOnlyList<ApiClient> result = null;

        Transaction(usersContext =>
        {
            var query = new GetApiClientsByApplicationIdQuery(usersContext);
            result = query.Execute(applicationId);
        });

        result.Count.ShouldBe(2);
    }

    private void LoadApiClients()
    {
        var vendor = new Vendor { VendorName = $"test vendor {Guid.NewGuid().ToString()}" };
        var odsInstance = new OdsInstance
        {
            Name = $"Test Instance {Guid.NewGuid().ToString()}",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        var application = new Application
        {
            ApplicationName = $"test app {Guid.NewGuid().ToString()}",
            ClaimSetName = "test claim set",
            Vendor = vendor,
            OperationalContextUri = OperationalContext.DefaultOperationalContextUri,
        };
        var user = new VendorUser
        {
            Email = "",
            FullName = application.ApplicationName,
            Vendor = vendor
        };

        var apiClient1 = new ApiClient
        {
            Application = application,
            Key = $"key {Guid.NewGuid().ToString()}",
            Secret = "secret",
            Name = application.ApplicationName,
            IsApproved = true,
            UseSandbox = false,
            KeyStatus = "Active",
            User = user,
        };

        var apiClient2 = new ApiClient
        {
            Application = application,
            Key = $"key {Guid.NewGuid().ToString()}",
            Secret = "secret",
            Name = $"{application.ApplicationName} 2",
            IsApproved = true,
            UseSandbox = false,
            KeyStatus = "Active",
            User = user,
        };

        var apiClientOdsIntance1 = new ApiClientOdsInstance
        {
            ApiClient = apiClient1,
            OdsInstance = odsInstance,
        };

        var apiClientOdsIntance2 = new ApiClientOdsInstance
        {
            ApiClient = apiClient2,
            OdsInstance = odsInstance,
        };

        Save(odsInstance, vendor, application, apiClient1, apiClient2, apiClientOdsIntance1, apiClientOdsIntance2);

        applicationId = application.ApplicationId;
    }
}
