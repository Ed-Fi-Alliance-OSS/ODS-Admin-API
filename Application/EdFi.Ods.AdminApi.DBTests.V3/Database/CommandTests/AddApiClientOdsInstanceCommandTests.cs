// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Moq;
using NUnit.Framework;
using Shouldly;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using EdFi.Ods.AdminApi.V3.Infrastructure.Helpers;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;

namespace EdFi.Ods.AdminApi.DBTests.V3.Database.CommandTests;

[TestFixture]
public class AddApiClientOdsInstanceCommandTests : PlatformUsersContextTestBase
{
    [Test]
    public void ShouldAddApiClient()
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

        var apiClientOdsInstance = new ApiClientOdsInstance
        {
            ApiClient = apiClient,
            OdsInstance = odsInstance,
        };
        Save(odsInstance, application, apiClient, apiClientOdsInstance);
        apiClientOdsInstance.ShouldNotBeNull();
    }
}



