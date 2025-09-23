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

public interface IGetVendorsQuery
{
    List<Vendor> Execute();
    List<Vendor> Execute(CommonQueryParams commonQueryParams);
}

public class GetVendorsQuery : IGetVendorsQuery
{
    private readonly IUsersContext _context;
    private readonly IOptions<AppSettings> _options;

    public GetVendorsQuery(IUsersContext context, IOptions<AppSettings> options)
    {
        _context = context;
        _options = options;
    }

    public List<Vendor> Execute()
    {
        return _context.Vendors
            .Include(vn => vn.VendorNamespacePrefixes)
            .Include(x => x.Users)
            .Include(x => x.Applications).ThenInclude(x => x.ApplicationEducationOrganizations)
            .Include(x => x.Applications).ThenInclude(x => x.Profiles)
            .Include(x => x.Applications).ThenInclude(x => x.OdsInstance)
            .OrderBy(v => v.VendorName).Where(v => v.VendorName != null && !VendorExtensions.ReservedNames.Contains(v.VendorName.Trim()))
            .ToList();
    }

    public List<Vendor> Execute(CommonQueryParams commonQueryParams)
    {
        return _context.Vendors
            .Include(vn => vn.VendorNamespacePrefixes)
            .Include(x => x.Users)
            .Include(x => x.Applications).ThenInclude(x => x.ApplicationEducationOrganizations)
            .Include(x => x.Applications).ThenInclude(x => x.Profiles)
            .Include(x => x.Applications).ThenInclude(x => x.OdsInstance)
            .OrderBy(v => v.VendorName).Where(v => v.VendorName != null && !VendorExtensions.ReservedNames.Contains(v.VendorName.Trim()))
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();
    }
}
