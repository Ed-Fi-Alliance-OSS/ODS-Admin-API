// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using System;

namespace EdFi.Ods.AdminApi.V3.DBTests.Database.QueryTests;

[TestFixture]
public class GetApiClientOdsInstanceQueryTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldGetApiClientOdsInstanceData()
    {
        var odsInstance = new OdsInstance
        {
            Name = "Test Instance",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };
        var application = new Application
        {
            ApplicationName = "test application",
            ClaimSetName = "test claim set",
            OperationalContextUri = OperationalContext.DefaultOperationalContextUri,
        };
        var apiClient = new ApiClient
        {
            Application = application,
            Key = "key",
            Secret = "secret",
            Name = application.ApplicationName,
            IsApproved = true,
            UseSandbox = false,
            KeyStatus = "Active"
        };

        var apiClientOdsIntance = new ApiClientOdsInstance
        {
            ApiClient = apiClient,
            OdsInstance = odsInstance,
        };
        Save(apiClientOdsIntance);
        int odsInstanceId = odsInstance.OdsInstanceId;
        int apiClientId = apiClient.ApiClientId;
        Transaction(usersContext =>
        {
            var getApiClientOdsInstanceQuery = new GetApiClientOdsInstanceQuery(usersContext);
            var results = getApiClientOdsInstanceQuery.Execute(apiClientId, odsInstanceId);
            results.ShouldNotBeNull();
        });
    }

    [Test]
    public void ShouldNotGetApiClientOdsInstanceWithDifferentApiClient()
    {
        var odsInstance = new OdsInstance
        {
            Name = "Test Instance",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };
        var application = new Application
        {
            ApplicationName = "test application",
            ClaimSetName = "test claim set",
            OperationalContextUri = OperationalContext.DefaultOperationalContextUri,
        };
        var apiClient = new ApiClient
        {
            Application = application,
            Key = "key",
            Secret = "secret",
            Name = application.ApplicationName,
            IsApproved = true,
            UseSandbox = false,
            KeyStatus = "Active"
        };

        var apiClientOdsIntance = new ApiClientOdsInstance
        {
            ApiClient = apiClient,
            OdsInstance = odsInstance,
        };
        Save(apiClientOdsIntance);
        int odsInstanceId = odsInstance.OdsInstanceId;
        int apiClientId = 999;
        Transaction(usersContext =>
        {
            var getApiClientOdsInstanceQuery = new GetApiClientOdsInstanceQuery(usersContext);
            var results = getApiClientOdsInstanceQuery.Execute(apiClientId, odsInstanceId);
            results.ShouldBeNull();
        });
    }

    [Test]
    public void ShouldNotGetApiClientOdsInstanceWithDifferentOdsInstance()
    {
        var odsInstance = new OdsInstance
        {
            Name = "Test Instance",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };
        var application = new Application
        {
            ApplicationName = "test application",
            ClaimSetName = "test claim set",
            OperationalContextUri = OperationalContext.DefaultOperationalContextUri,
        };
        var apiClient = new ApiClient
        {
            Application = application,
            Key = "key",
            Secret = "secret",
            Name = application.ApplicationName,
            IsApproved = true,
            UseSandbox = false,
            KeyStatus = "Active"
        };

        var apiClientOdsIntance = new ApiClientOdsInstance
        {
            ApiClient = apiClient,
            OdsInstance = odsInstance,
        };
        Save(apiClientOdsIntance);
        int odsInstanceId = 999;
        int apiClientId = apiClient.ApiClientId;
        Transaction(usersContext =>
        {
            var getApiClientOdsInstanceQuery = new GetApiClientOdsInstanceQuery(usersContext);
            var results = getApiClientOdsInstanceQuery.Execute(apiClientId, odsInstanceId);
            results.ShouldBeNull();
        });
    }
}



