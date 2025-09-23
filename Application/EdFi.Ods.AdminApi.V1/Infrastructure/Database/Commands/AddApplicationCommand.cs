// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Commands;

public interface IAddApplicationCommand
{
    AddApplicationResult Execute(IAddApplicationModel applicationModel);
}

public class AddApplicationCommand(IUsersContext usersContext, InstanceContext instanceContext) : IAddApplicationCommand
{
    private readonly IUsersContext _usersContext = usersContext;
    private readonly InstanceContext _instanceContext = instanceContext;

    public AddApplicationResult Execute(IAddApplicationModel applicationModel)
    {
        var profile = applicationModel.ProfileId.HasValue
            ? _usersContext.Profiles.SingleOrDefault(p => p.ProfileId == applicationModel.ProfileId.Value)
            : null;

        var vendor = _usersContext.Vendors.Include(x => x.Users)
            .Single(v => v.VendorId == applicationModel.VendorId);

        OdsInstance? odsInstance;

        if (_instanceContext != null && !string.IsNullOrEmpty(_instanceContext.Name))
        {
            odsInstance = _usersContext.OdsInstances.AsEnumerable().FirstOrDefault(x =>
                x.Name.Equals(_instanceContext.Name, StringComparison.InvariantCultureIgnoreCase));
        }
        else
        {
            odsInstance = _usersContext.OdsInstances.FirstOrDefault(o => o.OdsInstanceId == applicationModel.OdsInstanceId);
        }

        var user = vendor.Users.FirstOrDefault();

        var apiClient = new ApiClient(true)
        {
            Name = applicationModel.ApplicationName,
            IsApproved = true,
            UseSandbox = false,
            KeyStatus = "Active",
            User = user
        };

        var applicationEdOrgs = applicationModel.EducationOrganizationIds == null
            ? []
            : applicationModel.EducationOrganizationIds.Select(id => new ApplicationEducationOrganization
            {
                Clients = [apiClient],
                EducationOrganizationId = id
            });

        var application = new Application
        {
            ApplicationName = applicationModel.ApplicationName,
            ApiClients = [apiClient],
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>(applicationEdOrgs),
            ClaimSetName = applicationModel.ClaimSetName,
            Profiles = [],
            Vendor = vendor,
            OperationalContextUri = OperationalContext.DefaultOperationalContextUri,
            OdsInstance = odsInstance
        };

        if (profile != null)
        {
            application.Profiles.Add(profile);
        }

        _usersContext.Applications.Add(application);
        _usersContext.SaveChanges();

        return new AddApplicationResult
        {
            ApplicationId = application.ApplicationId,
            Key = apiClient.Key,
            Secret = apiClient.Secret
        };
    }
}

public interface IAddApplicationModel
{
    string ApplicationName { get; }
    int VendorId { get; }
    string? ClaimSetName { get; }
    int? ProfileId { get; }
    int? OdsInstanceId { get; }
    IEnumerable<int>? EducationOrganizationIds { get; }
}

public class AddApplicationResult
{
    public int ApplicationId { get; set; }
    public string? Key { get; set; }
    public string? Secret { get; set; }
}
