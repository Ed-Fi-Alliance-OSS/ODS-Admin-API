// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V2.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V2.Infrastructure.Database.Commands;

public interface IDeleteApplicationCommand
{
    void Execute(int id);
}

public class DeleteApplicationCommand : IDeleteApplicationCommand
{
    private readonly IUsersContext _context;

    public DeleteApplicationCommand(IUsersContext context)
    {
        _context = context;
    }

    public void Execute(int id)
    {
        var application = _context.Applications
            .Include(a => a.ApiClients)
            .Include(a => a.ApplicationEducationOrganizations)
            .Include(a => a.Profiles)
            .SingleOrDefault(a => a.ApplicationId == id) ?? throw new NotFoundException<int>("application", id);

        if (application != null && application.Vendor.IsSystemReservedVendor())
        {
            throw new ArgumentException("This Application is required for proper system function and may not be modified");
        }

        if (application == null)
        {
            return;
        }

        var currentClientAccessTokens = _context.ClientAccessTokens.Where(o => application.ApiClients.Contains(o.ApiClient));

        if (currentClientAccessTokens.Any())
        {
            _context.ClientAccessTokens.RemoveRange(currentClientAccessTokens);
        }

        var currentApiClientOdsInstances = _context.ApiClientOdsInstances.Where(o => application.ApiClients.Contains(o.ApiClient));

        if (currentApiClientOdsInstances.Any())
        {
            _context.ApiClientOdsInstances.RemoveRange(currentApiClientOdsInstances);
        }

        var currentApplicationEducationOrganizations = _context.ApplicationEducationOrganizations.Where(aeo => aeo.Application.ApplicationId == application.ApplicationId);

        if (currentApplicationEducationOrganizations.Any())
        {
            _context.ApplicationEducationOrganizations.RemoveRange(currentApplicationEducationOrganizations);
        }

        var currentApplicationClients = _context.ApiClients.AsEnumerable().Where(o => application.ApiClients.AsEnumerable().Any(oapp => oapp.ApiClientId == o.ApiClientId));

        if (currentApplicationClients.Any())
        {
            _context.ApiClients.RemoveRange(currentApplicationClients);
        }

        var currentProfiles = application.Profiles.ToList();

        if (currentProfiles.Any())
        {
            foreach (var profile in currentProfiles)
            {
                application.Profiles.Remove(profile);
            }
        }

        _context.Applications.Remove(application);
        _context.SaveChanges();
    }

}
