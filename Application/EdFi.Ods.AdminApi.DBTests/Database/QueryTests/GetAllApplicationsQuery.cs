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
public class GetAllApplicationsQueryTests : PlatformUsersContextTestBase
{
    private IOptions<AppSettings> _options { get; set; }

    [SetUp]
    public virtual async Task FixtureSetup()
    {
        _options = Testing.GetAppSettings();
        _options.Value.PreventDuplicateApplications = false;
        LoadApplications(3);
        await Task.Yield();
    }

    [Test]
    public void ShouldGetAllApplications()
    {
        var result = new List<Application>();

        Transaction(usersContext =>
        {
            var query = new GetAllApplicationsQuery(usersContext, _options);
            result = query.Execute(
                new CommonQueryParams(), null, null, null, null).ToList();
        });

        result.Count.ShouldBeGreaterThan(0);
    }

    [Test]
    public void ShouldGetAllApplications_With_Offset_and_Limit()
    {
        var offset = 1;
        var limit = 2;
        Transaction(usersContext =>
        {
            var query = new GetAllApplicationsQuery(usersContext, _options);
            var result = query.Execute(
                new CommonQueryParams(offset, limit), null, null, null, null);

            result.Count.ShouldBeGreaterThan(0);
            result.Count.ShouldBe(2);
        });
    }

    private void LoadApplications(int count = 1)
    {
        for (int i = 0; i < count; i++)
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

            var apiClient = new ApiClient
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

            var apiClientOdsIntance = new ApiClientOdsInstance
            {
                ApiClient = apiClient,
                OdsInstance = odsInstance,
            };

            Save(odsInstance, vendor, application, apiClient, apiClientOdsIntance);
        }
    }
}
