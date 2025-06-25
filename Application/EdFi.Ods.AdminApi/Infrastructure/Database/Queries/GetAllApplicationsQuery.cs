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
using EdFi.Ods.AdminApi.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetAllApplicationsQuery
{
    IReadOnlyList<Application> Execute(CommonQueryParams commonQueryParams, int? id, string? applicationName, string? claimsetName, string? ids);
}

public class GetAllApplicationsQuery : IGetAllApplicationsQuery
{
    private readonly IUsersContext _context;
    private readonly IOptions<AppSettings> _options;
    private readonly Dictionary<string, Expression<Func<Application, object>>> _orderByColumnApplications;

    public GetAllApplicationsQuery(IUsersContext context, IOptions<AppSettings> options)
    {
        _context = context;
        _options = options;
        var isSQLServerEngine = _options.Value.DatabaseEngine?.ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant();
        _orderByColumnApplications = new Dictionary<string, Expression<Func<Application, object>>>
        (StringComparer.OrdinalIgnoreCase)
        {
            { SortingColumns.ApplicationNameColumn, x => isSQLServerEngine ? EF.Functions.Collate(x.ApplicationName, DatabaseEngineEnum.SqlServerCollation) : x.ApplicationName },
            { SortingColumns.ApplicationClaimSetNameColumn, x => isSQLServerEngine ? EF.Functions.Collate(x.ClaimSetName, DatabaseEngineEnum.SqlServerCollation) : x.ClaimSetName },
            { SortingColumns.DefaultIdColumn, x => x.ApplicationId }
        };
    }

    public IReadOnlyList<Application> Execute(CommonQueryParams commonQueryParams, int? id, string? applicationName, string? claimsetName, string? ids)
    {
        Expression<Func<Application, object>> columnToOrderBy = _orderByColumnApplications.GetColumnToOrderBy(commonQueryParams.OrderBy);

        var applications = _context.Applications
            .Include(a => a.ApplicationEducationOrganizations)
            .Include(a => a.Profiles)
            .Include(a => a.Vendor)
            .Include(a => a.ApiClients)
            .Where(a => id == null || a.ApplicationId == id)
            .Where(a => applicationName == null || a.ApplicationName == applicationName)
            .Where(a => claimsetName == null || a.ClaimSetName == claimsetName)
            .Where(a => ids == null || ids.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).Contains(a.ApplicationId))
            .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();

        return applications;
    }
}
