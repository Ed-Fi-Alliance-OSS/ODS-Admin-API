// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public class ValidateApplicationExistsQuery(IUsersContext context)
{
    private readonly IUsersContext _context = context;

    public bool Execute(IAddApplicationModel applicationModel)
    {
        var existingApplication = _context.Applications
                .Include(a => a.ApplicationEducationOrganizations)
                .Include(a => a.Profiles)
                .Include(a => a.Vendor)
                .Include(a => a.ApiClients)
                .Where(a => a.ApplicationName == applicationModel.ApplicationName
                    && a.Vendor.VendorId == applicationModel.VendorId
                    && a.ClaimSetName == applicationModel.ClaimSetName
                    && (((a.ApplicationEducationOrganizations == null
                            || (a.ApplicationEducationOrganizations != null && !a.ApplicationEducationOrganizations.Any()))
                        && (applicationModel.EducationOrganizationIds == null
                            || (applicationModel.EducationOrganizationIds != null && !applicationModel.EducationOrganizationIds.Any())))
                        || ((a.ApplicationEducationOrganizations != null && applicationModel.EducationOrganizationIds != null)
                               && ((!(a.ApplicationEducationOrganizations.Any() || applicationModel.EducationOrganizationIds.Any()))
                               || ((a.ApplicationEducationOrganizations.Any() && applicationModel.EducationOrganizationIds.Any())
                               && (a.ApplicationEducationOrganizations.All(b => applicationModel.EducationOrganizationIds.Contains(b.EducationOrganizationId)))
                               && (a.ApplicationEducationOrganizations.Count == applicationModel.EducationOrganizationIds.Count()))
                            ))
                        )).AsEnumerable()
                .Where(b =>
                    ((b.Profiles == null
                        || (b.Profiles != null && b.Profiles.Count == 0))
                    && (applicationModel.ProfileIds == null
                        || (applicationModel.ProfileIds != null && applicationModel.ProfileIds.Any())))
                    || ((b.Profiles != null && applicationModel.ProfileIds != null)
                        && (!(b.Profiles.Count != 0 || applicationModel.ProfileIds.Any())
                            || ((b.Profiles.Count != 0 && applicationModel.ProfileIds.Any())
                                && (b.Profiles.Count == applicationModel.ProfileIds.Count())
                                && (b.Profiles.All(c => applicationModel.ProfileIds.Contains(c.ProfileId)))
                                )
                            )
                        )
                    ).Select(
                    applications => new
                    {
                        applications.ApplicationName,
                        applications.Vendor.VendorId,
                        ProfileIds = applications.Profiles.Select(k => k.ProfileId).ToList(),
                        EducationOrganizationIds = applications.ApplicationEducationOrganizations.Select(k => k.EducationOrganizationId).ToList(),
                        applications.ClaimSetName,
                        ApiClients = applications.ApiClients.Select(k => k.Application.ApplicationId).ToList(),
                    }).ToList();

        var existingInstance = _context.ApiClientOdsInstances
                .Include(x => x.ApiClient)
                .ThenInclude(app => app.Application)
                .Where(a => a.ApiClient.Application.ApplicationName == applicationModel.ApplicationName
                    && a.ApiClient.Application.Vendor.VendorId == applicationModel.VendorId
                    && a.ApiClient.Application.ClaimSetName == applicationModel.ClaimSetName)
                .Select(m => new
                {
                    m.ApiClient.Application.ApplicationId,
                    m.OdsInstance.OdsInstanceId
                }).ToList();
        if (existingApplication.Count != 0)
        {
            if ((existingInstance == null
                || (existingInstance != null
                    && existingInstance.Count == 0))
             && (applicationModel.OdsInstanceIds == null
                || (applicationModel.OdsInstanceIds != null && !applicationModel.OdsInstanceIds.Any()))
              )
            {
                return true;
            }
            bool instance = existingApplication.Exists(
            x => x.ApiClients.Exists(y => existingInstance != null && existingInstance.Exists(z => z.ApplicationId == y))
            );
            return instance;
        }
        return false;
    }
}
