// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V1.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;
public interface IGetApplicationsQuery
{
    List<Application> Execute();
    List<Application> Execute(CommonQueryParams commonQueryParams);
}

public class GetApplicationsQuery : IGetApplicationsQuery
{
    private readonly IUsersContext _context;
    private readonly IOptions<AppSettings> _options;

    public GetApplicationsQuery(IUsersContext context, IOptions<AppSettings> options)
    {
        _context = context;
        _options = options;
    }

    public List<Application> Execute()
    {
        return _context.Applications
            .Include(ap => ap.Vendor!).ThenInclude(ap => ap.VendorNamespacePrefixes)
            .Include(ap => ap.Vendor!).ThenInclude(ap => ap.Users)
            .Include(ap => ap.Profiles)
            .Include(ap => ap.OdsInstance)
            .Include(ap => ap.ApplicationEducationOrganizations)
            .OrderBy(v => v.Vendor!.VendorName)
            .Where(v => v.Vendor != null && v.Vendor.VendorName != null && !VendorExtensions.ReservedNames.Contains(v.Vendor.VendorName.Trim()))
            .ToList();
    }

    public List<Application> Execute(CommonQueryParams commonQueryParams)
    {
        return _context.Applications
            .Include(ap => ap.Vendor!).ThenInclude(ap => ap.VendorNamespacePrefixes)
            .Include(ap => ap.Vendor!).ThenInclude(ap => ap.Users)
            .Include(ap => ap.Profiles)
            .Include(ap => ap.OdsInstance)
            .Include(ap => ap.ApplicationEducationOrganizations)
            .OrderBy(v => v.ApplicationName)
            .Where(v => v.Vendor != null && v.Vendor.VendorName != null && !VendorExtensions.ReservedNames.Contains(v.Vendor.VendorName.Trim()))
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();
    }
}
