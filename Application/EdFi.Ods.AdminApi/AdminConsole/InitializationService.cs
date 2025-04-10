// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Dynamic;
using AutoMapper;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Queries;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Applications;
using EdFi.Ods.AdminApi.Features.OdsInstanceContext;
using EdFi.Ods.AdminApi.Features.OdsInstanceDerivative;
using EdFi.Ods.AdminApi.Features.ODSInstances;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using log4net;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using static EdFi.Ods.AdminApi.AdminConsole.Features.Instances.AddInstance;
using static EdFi.Ods.AdminApi.Features.Applications.AddApplication;
using static EdFi.Ods.AdminApi.Features.Vendors.AddVendor;

namespace EdFi.Ods.AdminApi.AdminConsole;

public interface IAdminConsoleInitializationService
{
    Task<int> InitializeApplications(WebApplication app);
}
public class InitializationService : IAdminConsoleInitializationService
{
    private readonly IUsersContext _usersContext;
    private readonly IGetApplicationByNameAndClaimsetQuery _getApplicationByNameAndClaimset;
    private readonly IOptions<AppSettings> _options;

    public InitializationService(IUsersContext usersContext,
        IGetApplicationByNameAndClaimsetQuery getApplicationByNameAndClaimset,
        IOptions<AppSettings> options)
    {
        _usersContext = usersContext;
        _getApplicationByNameAndClaimset = getApplicationByNameAndClaimset;
        _options = options;
    }

    public Task<int> InitializeApplications(WebApplication app)
    {
        int applicationId = 0;
        string adminConsoleApplicationName = app.Configuration.GetValue<string>("AdminConsoleSettings:ApplicationName") ?? String.Empty;
        string adminConsoleClaimSetName = app.Configuration.GetValue<string>("AdminConsoleSettings:ClaimsetName") ?? String.Empty;
        var adminConsoleApplication = _getApplicationByNameAndClaimset.Execute(adminConsoleApplicationName, adminConsoleClaimSetName);
        // if application and vendor do not exist, a new app is added
        if (adminConsoleApplication == null)
        {
            // Create the vendor
            Vendor vendor = InitializeVendor(app);
            var command = new AddApplicationCommand(_usersContext);
            // Create the application
            var newApplication = new AddApplicationRequest
            {
                ApplicationName = adminConsoleApplicationName,
                ClaimSetName = adminConsoleClaimSetName,
                ProfileIds = null,
                VendorId = vendor?.VendorId ?? 0
            };
            var result = command.Execute(newApplication, _options);
            applicationId = result.ApplicationId;
        }
        else
        {
            applicationId = adminConsoleApplication.ApplicationId;
        }
        return Task.FromResult(applicationId);
    }

    private Vendor InitializeVendor(WebApplication app)
    {
        var command = new AddVendorCommand(_usersContext);
        string vendorCompany = app.Configuration.GetValue<string>("AdminConsoleSettings:VendorCompany") ?? String.Empty;
        string vendorContactEmailAddress = app.Configuration.GetValue<string>("AdminConsoleSettings:VendorContactEmailAddress") ?? String.Empty;
        string vendorContactName = app.Configuration.GetValue<string>("AdminConsoleSettings:VendorContactName") ?? String.Empty;
        string vendorNamespacePrefixes = app.Configuration.GetValue<string>("AdminConsoleSettings:VendorNamespacePrefixes") ?? String.Empty;
        IAddVendorModel vendor = new AddVendorRequest
        {
            Company = vendorCompany,
            NamespacePrefixes = vendorNamespacePrefixes,
            ContactName = vendorContactName,
            ContactEmailAddress = vendorContactEmailAddress
        };
        return command.Execute(vendor);
    }

}


