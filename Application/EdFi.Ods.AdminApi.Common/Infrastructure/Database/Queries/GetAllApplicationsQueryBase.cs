// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetAllApplicationsQueryBase(IUsersContext context, IOptions<AppSettings> options)
{
    private readonly IUsersContext _context = context;
    private readonly IOptions<AppSettings> _options = options;
    private readonly Dictionary<string, Expression<Func<Application, object>>> _orderByColumnApplications =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { SortingColumns.ApplicationNameColumn, x => (options.Value.DatabaseEngine ?? string.Empty).ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant() ? EF.Functions.Collate(x.ApplicationName, DatabaseEngineEnum.SqlServerCollation) : x.ApplicationName },
            { SortingColumns.ApplicationClaimSetNameColumn, x => (options.Value.DatabaseEngine ?? string.Empty).ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant() ? EF.Functions.Collate(x.ClaimSetName, DatabaseEngineEnum.SqlServerCollation) : x.ClaimSetName },
            { SortingColumns.DefaultIdColumn, x => x.ApplicationId }
        };

    protected IReadOnlyList<Application> ExecuteCore(CommonQueryParams commonQueryParams, int? id, string? applicationName, string? claimsetName, string? ids)
    {
        Expression<Func<Application, object>> columnToOrderBy = _orderByColumnApplications.GetColumnToOrderBy(commonQueryParams.OrderBy);

        return _context.Applications
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
    }
}
