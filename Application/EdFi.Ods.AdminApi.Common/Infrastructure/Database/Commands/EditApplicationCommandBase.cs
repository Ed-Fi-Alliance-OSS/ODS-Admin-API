// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Commands;

public abstract class EditApplicationCommandBase(IUsersContext context)
{
    private readonly IUsersContext _context = context;

    protected Application ExecuteCore(
        int id,
        string? applicationName,
        int vendorId,
        string? claimSetName,
        IEnumerable<int>? profileIds,
        IEnumerable<long>? educationOrganizationIds,
        IEnumerable<int>? odsInstanceIds,
        bool? enabled)
    {
        var application = _context.Applications
            .Include(a => a.ApplicationEducationOrganizations)
            .Include(a => a.Profiles)
            .Include(a => a.Vendor)
            .Include(a => a.ApiClients)
            .SingleOrDefault(a => a.ApplicationId == id) ?? throw new NotFoundException<int>("application", id);

        if (application.Vendor.IsSystemReservedVendor())
        {
            throw new AdminApiException("This Application is required for proper system function and may not be modified");
        }

        var newVendor = _context.Vendors.Single(v => v.VendorId == vendorId);
        var newProfiles = profileIds != null
            ? _context.Profiles.Where(p => profileIds.Contains(p.ProfileId))
            : null;
        var newOdsInstances = odsInstanceIds != null
            ? _context.OdsInstances.Where(p => odsInstanceIds.Contains(p.OdsInstanceId))
            : null;

        var apiClient = application.ApiClients.Single();
        var currentApiClientId = apiClient.ApiClientId;
        apiClient.Name = applicationName;
        apiClient.IsApproved = enabled ?? true;

        _context.ApiClientOdsInstances.RemoveRange(_context.ApiClientOdsInstances.Where(o => o.ApiClient.ApiClientId == currentApiClientId));
        _context.ApplicationEducationOrganizations.RemoveRange(_context.ApplicationEducationOrganizations.Where(aeo => aeo.Application.ApplicationId == application.ApplicationId));

        var currentProfiles = application.Profiles.ToList();
        foreach (var profile in currentProfiles)
        {
            application.Profiles.Remove(profile);
        }

        application.ApplicationName = applicationName;
        application.ClaimSetName = claimSetName;
        application.Vendor = newVendor;

        var newApplicationEdOrgs = educationOrganizationIds == null
            ? []
            : educationOrganizationIds.Select(educationOrganizationId => new ApplicationEducationOrganization
            {
                ApiClients = new List<ApiClient> { apiClient },
                EducationOrganizationId = educationOrganizationId,
                Application = application,
            });

        foreach (var appEdOrg in newApplicationEdOrgs)
        {
            application.ApplicationEducationOrganizations.Add(appEdOrg);
        }

        application.Profiles ??= [];
        application.Profiles.Clear();

        if (newProfiles != null)
        {
            foreach (var profile in newProfiles)
            {
                application.Profiles.Add(profile);
            }
        }

        if (newOdsInstances != null)
        {
            foreach (var newOdsInstance in newOdsInstances)
            {
                _context.ApiClientOdsInstances.Add(new ApiClientOdsInstance { ApiClient = apiClient, OdsInstance = newOdsInstance });
            }
        }

        _context.SaveChanges();
        return application;
    }
}
