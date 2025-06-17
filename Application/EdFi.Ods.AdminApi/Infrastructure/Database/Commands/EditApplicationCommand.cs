// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

public interface IEditApplicationCommand
{
    Application Execute(IEditApplicationModel model);
}

public class EditApplicationCommand : IEditApplicationCommand
{
    private readonly IUsersContext _context;

    public EditApplicationCommand(IUsersContext context)
    {
        _context = context;
    }

    public Application Execute(IEditApplicationModel model)
    {
        var application = _context.Applications
            .Include(a => a.ApplicationEducationOrganizations)
            .Include(a => a.Profiles)
            .Include(a => a.Vendor)
            .Include(a => a.ApiClients)
            .SingleOrDefault(a => a.ApplicationId == model.Id) ?? throw new NotFoundException<int>("application", model.Id);

        if (application.Vendor.IsSystemReservedVendor())
        {
            throw new AdminApiException("This Application is required for proper system function and may not be modified");
        }

        var newVendor = _context.Vendors.Single(v => v.VendorId == model.VendorId);
        var newProfiles = model.ProfileIds != null
            ? _context.Profiles.Where(p => model.ProfileIds.Contains(p.ProfileId))
            : null;
        var newOdsInstances = model.OdsInstanceIds != null
            ? _context.OdsInstances.Where(p => model.OdsInstanceIds.Contains(p.OdsInstanceId))
            : null;

        var apiClient = application.ApiClients.Single();
        var currentApiClientId = apiClient.ApiClientId;
        apiClient.Name = model.ApplicationName;
        apiClient.IsApproved = model.Enabled ?? true;

        _context.ApiClientOdsInstances.RemoveRange(_context.ApiClientOdsInstances.Where(o => o.ApiClient.ApiClientId == currentApiClientId));
        _context.ApplicationEducationOrganizations.RemoveRange(_context.ApplicationEducationOrganizations.Where(aeo => aeo.Application.ApplicationId == application.ApplicationId));

        var currentProfiles = application.Profiles.ToList();
        foreach (var profile in currentProfiles)
        {
            application.Profiles.Remove(profile);
        }


        application.ApplicationName = model.ApplicationName;
        application.ClaimSetName = model.ClaimSetName;
        application.Vendor = newVendor;

        var newApplicationEdOrgs = model.EducationOrganizationIds == null
            ? []
            : model.EducationOrganizationIds.Select(id => new ApplicationEducationOrganization
            {
                ApiClients = new List<ApiClient> { apiClient },
                EducationOrganizationId = id,
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

public interface IEditApplicationModel
{
    int Id { get; }
    string? ApplicationName { get; }
    int VendorId { get; }
    string? ClaimSetName { get; }
    IEnumerable<int>? ProfileIds { get; }
    IEnumerable<long>? EducationOrganizationIds { get; }
    IEnumerable<int>? OdsInstanceIds { get; }
    bool? Enabled { get; }
}
