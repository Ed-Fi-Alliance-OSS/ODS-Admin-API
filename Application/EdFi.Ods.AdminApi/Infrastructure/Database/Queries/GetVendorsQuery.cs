// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetVendorsQuery
{
    List<Vendor> Execute();
    List<Vendor> Execute(CommonQueryParams commonQueryParams, int? id, string? company, string? namespacePrefixes, string? contactName, string? contactEmailAddress);
}

public class GetVendorsQuery : IGetVendorsQuery
{
    private readonly IUsersContext _context;
    private readonly IOptions<AppSettings> _options;
    private readonly Dictionary<string, Expression<Func<Vendor, object>>> _orderByColumnVendors;
    public GetVendorsQuery(IUsersContext context, IOptions<AppSettings> options)
    {
        _context = context;
        _options = options;
        var isSQLServerEngine = _options.Value.DatabaseEngine?.ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant();
        _orderByColumnVendors = new Dictionary<string, Expression<Func<Vendor, object>>>
            (StringComparer.OrdinalIgnoreCase)
        {
            { SortingColumns.VendorCompanyColumn, x => isSQLServerEngine ? EF.Functions.Collate(x.VendorName, DatabaseEngineEnum.SqlServerCollation) : x.VendorName },
            #pragma warning disable CS8602 // Dereference of a possibly null reference.
            { SortingColumns.VendorContactNameColumn, x => isSQLServerEngine ? EF.Functions.Collate(x.Users.FirstOrDefault().FullName, DatabaseEngineEnum.SqlServerCollation) : x.Users.FirstOrDefault().FullName },
            { SortingColumns.VendorContactEmailColumn, x => isSQLServerEngine ? EF.Functions.Collate(x.Users.FirstOrDefault().Email, DatabaseEngineEnum.SqlServerCollation) : x.Users.FirstOrDefault().Email },
            #pragma warning restore CS8602 // Dereference of a possibly null reference.
            { SortingColumns.VendorNamespacePrefixesColumn, x => isSQLServerEngine ? EF.Functions.Collate(x.VendorNamespacePrefixes.OrderBy(p => p.NamespacePrefix).First().NamespacePrefix, DatabaseEngineEnum.SqlServerCollation) : x.VendorNamespacePrefixes.OrderBy(p => p.NamespacePrefix).First().NamespacePrefix },
            { SortingColumns.DefaultIdColumn, x => x.VendorId }
        };
    }

    public List<Vendor> Execute()
    {
        return _context.Vendors
            .Include(v => v.Applications)
                .ThenInclude(a => a.Profiles)
            .Include(v => v.Applications)
                .ThenInclude(a => a.ApplicationEducationOrganizations)
            .Include(v => v.Applications)
                .ThenInclude(a => a.ApiClients)
            .Include(v => v.Users)
            .Include(v => v.VendorNamespacePrefixes)
            .OrderBy(v => v.VendorName).Where(v => !VendorExtensions.ReservedNames.Contains(v.VendorName.Trim())).ToList();
    }

    public List<Vendor> Execute(CommonQueryParams commonQueryParams, int? id, string? company, string? namespacePrefixes, string? contactName, string? contactEmailAddress)
    {
        Expression<Func<Vendor, object>> columnToOrderBy = _orderByColumnVendors.GetColumnToOrderBy(commonQueryParams.OrderBy);

        return _context.Vendors
            .Include(v => v.Applications)
                .ThenInclude(a => a.Profiles)
            .Include(v => v.Applications)
                .ThenInclude(a => a.ApplicationEducationOrganizations)
            .Include(v => v.Applications)
                .ThenInclude(a => a.ApiClients)
            .Include(v => v.Users)
            .Include(v => v.VendorNamespacePrefixes)
            .Where(v => !VendorExtensions.ReservedNames.Contains(v.VendorName.Trim()))
            .Where(c => id == null || id < 1 || c.VendorId == id)
            .Where(c => company == null || c.VendorName == company)
            .Where(c => namespacePrefixes == null || c.VendorNamespacePrefixes.Any(v => v.NamespacePrefix == namespacePrefixes))
            .Where(c => c.Users.Any(u => contactName == null || u.FullName == contactName))
            .Where(c => c.Users.Any(u => contactEmailAddress == null || u.Email == contactEmailAddress))
            .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();
    }
}
