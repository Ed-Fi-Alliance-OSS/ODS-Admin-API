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
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using VendorUser = EdFi.Admin.DataAccess.Models.User;

namespace EdFi.Ods.AdminApi.DBTests.Database.CommandTests;

[TestFixture]
internal class EditApiClientCommandTests : PlatformUsersContextTestBase
{
    private Vendor _vendor;
    private VendorUser _user;
    private ApiClient _apiClient;
    private Application _application;
    private OdsInstance _odsInstance;
    private ApiClientOdsInstance _apiClientOdsInstance;

    private IOptions<AppSettings> _options { get; set; }

    [SetUp]
    public new virtual async Task SetUp()
    {
        AppSettings appSettings = new()
        {
            PreventDuplicateApplications = false
        };
        _options = Options.Create(appSettings);
        await Task.Yield();
    }

    private void SetupTestEntities()
    {
        _odsInstance = new OdsInstance
        {
            Name = "Test Instance",
            InstanceType = "Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        _vendor = new Vendor
        {
            VendorNamespacePrefixes = [new() { NamespacePrefix = "http://tests.com" }],
            VendorName = "Integration Tests"
        };

        _user = new VendorUser
        {
            Email = "nobody@nowhere.com",
            FullName = "Integration Tests",
            Vendor = _vendor
        };

        _apiClient = new ApiClient(true)
        {
            Name = "Integration Test",
            IsApproved = true,
            UseSandbox = false,
        };

        _apiClientOdsInstance = new ApiClientOdsInstance
        {
            OdsInstance = _odsInstance,
            ApiClient = _apiClient
        };

        _application = new Application
        {
            ApplicationName = "Test Application",
            ClaimSetName = "FakeClaimSet",
            Vendor = _vendor,
            OperationalContextUri = OperationalContext.DefaultOperationalContextUri
        };

        _application.ApiClients.Add(_apiClient);

        Save(_vendor, _user, _application, _odsInstance, _apiClientOdsInstance);
    }

    [Test]
    public void ShouldEditName()
    {
        SetupTestEntities();

        var editModel = new TestEditApplicationModel
        {
            Id = _apiClient.ApiClientId,
            ApplicationId = _application.ApplicationId,
            Name = $"{_apiClient.Name}_Edited",
            IsApproved = true,
            OdsInstanceIds = null
        };

        Transaction(usersContext =>
        {
            var command = new EditApiClientCommand(usersContext);
            command.Execute(editModel);
        });

        Transaction(usersContext =>
        {
            var apiClientOdsInstances = usersContext.ApiClients.First(ac => ac.ApiClientId == _apiClient.ApiClientId);
            apiClientOdsInstances.Name.ShouldBe($"{_apiClient.Name}_Edited");
        });
    }

    [Test]
    public void ShouldEditIsApproved()
    {
        SetupTestEntities();

        var editModel = new TestEditApplicationModel
        {
            Id = _apiClient.ApiClientId,
            ApplicationId = _application.ApplicationId,
            Name = _apiClient.Name,
            IsApproved = false,
            OdsInstanceIds = null
        };

        Transaction(usersContext =>
        {
            var command = new EditApiClientCommand(usersContext);
            command.Execute(editModel);
        });

        Transaction(usersContext =>
        {
            var apiClientOdsInstances = usersContext.ApiClients.First(ac => ac.ApiClientId == _apiClient.ApiClientId);
            apiClientOdsInstances.IsApproved.ShouldBe(false);
        });
    }

    [Test]
    public void ShouldRemoveOdsInstancesIfNull()
    {
        SetupTestEntities();

        var editModel = new TestEditApplicationModel
        {
            Id = _apiClient.ApiClientId,
            ApplicationId = _application.ApplicationId,
            Name = _apiClient.Name,
            IsApproved = true,
            OdsInstanceIds = null
        };

        Transaction(usersContext =>
        {
            var command = new EditApiClientCommand(usersContext);
            command.Execute(editModel);
        });

        Transaction(usersContext =>
        {
            var apiClientOdsInstances = usersContext.ApiClientOdsInstances.Where(oi => oi.ApiClient.ApiClientId == _apiClient.ApiClientId);
            apiClientOdsInstances.ShouldBeEmpty();
        });
    }

    [Test]
    public void ShouldAddOdsInstancesIfNew()
    {
        SetupTestEntities();

        var _newOdsInstance = new OdsInstance
        {
            Name = "Other Test Instance",
            InstanceType = "other Ods",
            ConnectionString = "Data Source=(local);Initial Catalog=EdFi_Ods;Integrated Security=True;Encrypt=False"
        };

        Save(_newOdsInstance);

        var editModel = new TestEditApplicationModel
        {
            Id = _apiClient.ApiClientId,
            ApplicationId = _application.ApplicationId,
            Name = _apiClient.Name,
            IsApproved = true,
            OdsInstanceIds = [_odsInstance.OdsInstanceId, _newOdsInstance.OdsInstanceId]
        };

        Transaction(usersContext =>
        {
            var command = new EditApiClientCommand(usersContext);
            command.Execute(editModel);
        });

        Transaction(usersContext =>
        {
            var apiClientOdsInstances = usersContext.ApiClientOdsInstances.Where(oi => oi.ApiClient.ApiClientId == _apiClient.ApiClientId);
            apiClientOdsInstances.Count().ShouldBe(2);
        });
    }
    private class TestEditApplicationModel : IEditApiClientModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsApproved { get; set; }
        public int ApplicationId { get; set; }
        public IEnumerable<int> OdsInstanceIds { get; set; }
    }
}
