// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Common.Utils.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Commands;

public interface IEditApplicationCommand
{
    Application Execute(IEditApplicationModel model);
}

public class EditApplicationCommand(IUsersContext context) : IEditApplicationCommand
{
    private readonly IUsersContext _context = context;

    public Application Execute(IEditApplicationModel model)
    {
        var application = _context.Applications
            .Include(a => a.Vendor)
            .Include(a => a.ApplicationEducationOrganizations)
            .Include(a => a.ApiClients)
            .Include(a => a.Profiles)
            .SingleOrDefault(a => a.ApplicationId == model.ApplicationId) ?? throw new NotFoundException<int>("application", model.ApplicationId);

        if (application.Vendor != null && application.Vendor.IsSystemReservedVendor())
        {
            throw new Exception("This Application is required for proper system function and may not be modified");
        }

        var newVendor = _context.Vendors.Single(v => v.VendorId == model.VendorId);
        var newProfile = model.ProfileId.HasValue
            ? _context.Profiles.Single(p => p.ProfileId == model.ProfileId.Value)
            : null;

        var apiClient = application.ApiClients.Single();
        apiClient.Name = model.ApplicationName;

        application.ApplicationName = model.ApplicationName;
        application.ClaimSetName = model.ClaimSetName;
        application.Vendor = newVendor;

        application.ApplicationEducationOrganizations ??= [];

        // Quick and dirty: simply remove all existing links to ApplicationEducationOrganizations...
        application.ApplicationEducationOrganizations.ToList().ForEach(x => _context.ApplicationEducationOrganizations.Remove(x));
        application.ApplicationEducationOrganizations.Clear();
        // ... and now create the new proper list.
        model.EducationOrganizationIds?.ForEach(id => application.ApplicationEducationOrganizations.Add(application.CreateApplicationEducationOrganization(id)));

        application.Profiles ??= [];

        application.Profiles.Clear();

        if (newProfile != null)
        {
            application.Profiles.Add(newProfile);
        }

        _context.SaveChanges();
        return application;
    }
}

public interface IEditApplicationModel
{
    int ApplicationId { get; }
    string ApplicationName { get; }
    int VendorId { get; }
    string? ClaimSetName { get; }
    int? ProfileId { get; }
    int? OdsInstanceId { get; }
    IEnumerable<int>? EducationOrganizationIds { get; }
}
