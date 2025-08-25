// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;

public class GetApplicationsByVendorIdQuery
{
    private readonly IUsersContext _context;

    public GetApplicationsByVendorIdQuery(IUsersContext context)
    {
        _context = context;
    }

    public List<Application> Execute(int vendorid)
    {
        var applications = _context.Applications
            .Include(x=> x.Profiles)
            .Include(x => x.OdsInstance)
            .Include(x => x.ApplicationEducationOrganizations)
            .Include(x => x.Vendor)
            .Where(a => a.Vendor != null && a.Vendor.VendorId == vendorid)
            .ToList();

        if (!applications.Any() && _context.Vendors.Find(vendorid) == null)
        {
            throw new NotFoundException<int>("vendor", vendorid);
        }

        return applications;
    }
}
