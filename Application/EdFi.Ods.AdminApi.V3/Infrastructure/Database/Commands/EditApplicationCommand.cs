// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

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

        // ADMINAPI-1484: enabled and data-store assignment are per-ApiClient concerns.
        // PUT /v3/applications/{id} does not read or apply either one - use
        // PUT /v3/apiClients/{id} to change an individual credential's approval state or
        // data-store assignment. Name is likewise never assigned: a credential's name
        // belongs to the credential and must survive an Application edit.
        var apiClients = application.ApiClients.ToList();

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
            : model.EducationOrganizationIds.Distinct().Select(id => new ApplicationEducationOrganization
            {
                // ADMINAPI-1514: education-organization scope is Application-level, so every
                // credential of the Application holds it. Each row gets its own list instance.
                ApiClients = apiClients.ToList(),
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
}



