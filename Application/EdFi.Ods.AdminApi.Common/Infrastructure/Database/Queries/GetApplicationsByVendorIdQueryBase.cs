// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetApplicationsByVendorIdQueryBase(IUsersContext context)
{
    private readonly IUsersContext _context = context;

    protected List<Application> ExecuteCore(int vendorId)
    {
        var applications = _context.Applications
            .Include(a => a.ApplicationEducationOrganizations)
            .Include(a => a.Profiles)
            .Include(a => a.Vendor)
            .Include(a => a.ApiClients)
            .Where(a => a.Vendor != null && a.Vendor.VendorId == vendorId)
            .ToList();

        if (!applications.Any() && _context.Vendors.Find(vendorId) == null)
        {
            throw new NotFoundException<int>("vendor", vendorId);
        }

        return applications;
    }
}
