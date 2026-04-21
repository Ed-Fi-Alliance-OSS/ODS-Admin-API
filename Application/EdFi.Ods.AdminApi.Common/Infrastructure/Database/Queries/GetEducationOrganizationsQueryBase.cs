// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetEducationOrganizationsQueryBase
{
    private readonly AdminApiDbContext _adminApiDbContext;
    private readonly IOptions<AppSettings> _options;
    private readonly Dictionary<string, Expression<Func<EducationOrganization, object>>> _orderByColumns;

    protected GetEducationOrganizationsQueryBase(AdminApiDbContext adminApiDbContext, IOptions<AppSettings> options)
    {
        _adminApiDbContext = adminApiDbContext;
        _options = options;

        var isSQLServerEngine = _options.Value.DatabaseEngine?.ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant();

        _orderByColumns = new Dictionary<string, Expression<Func<EducationOrganization, object>>>(StringComparer.OrdinalIgnoreCase)
        {
            { "educationOrganizationId", x => x.EducationOrganizationId },
            {
                "nameOfInstitution", x => isSQLServerEngine
                    ? EF.Functions.Collate(x.NameOfInstitution, DatabaseEngineEnum.SqlServerCollation)
                    : x.NameOfInstitution
            },
            {
                "discriminator", x => isSQLServerEngine
                    ? EF.Functions.Collate(x.Discriminator, DatabaseEngineEnum.SqlServerCollation)
                    : x.Discriminator
            },
            { "instanceId", x => x.InstanceId },
            { "lastRefreshed", x => x.LastRefreshed },
            { SortingColumns.DefaultIdColumn, x => x.Id }
        };
    }

    protected async Task<List<OdsInstanceWithEducationOrganizationsModel>> ExecuteCoreAsync()
    {
        var educationOrganizations = await _adminApiDbContext.EducationOrganizations
            .OrderBy(e => e.InstanceId)
            .ThenBy(e => e.EducationOrganizationId)
            .ToListAsync();

        return GroupEducationOrganizationsByInstance(educationOrganizations);
    }

    protected async Task<List<OdsInstanceWithEducationOrganizationsModel>> ExecuteCoreAsync(CommonQueryParams commonQueryParams, int? instanceId)
    {
        Expression<Func<EducationOrganization, object>> columnToOrderBy =
            _orderByColumns.GetColumnToOrderBy(commonQueryParams.OrderBy);

        var educationOrganizations = await _adminApiDbContext.EducationOrganizations
            .Where(e => instanceId == null || e.InstanceId == instanceId)
            .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToListAsync();

        return GroupEducationOrganizationsByInstance(educationOrganizations);
    }

    private static List<OdsInstanceWithEducationOrganizationsModel> GroupEducationOrganizationsByInstance(
        List<EducationOrganization> educationOrganizations)
    {
        return educationOrganizations
            .GroupBy(e => new { e.InstanceId, e.InstanceName })
            .Select(group => new OdsInstanceWithEducationOrganizationsModel
            {
                Id = group.Key.InstanceId,
                Name = group.Key.InstanceName ?? string.Empty,
                InstanceType = null,
                EducationOrganizations = group
                    .Select(e => new EducationOrganizationItemModel
                    {
                        EducationOrganizationId = e.EducationOrganizationId,
                        NameOfInstitution = e.NameOfInstitution,
                        ShortNameOfInstitution = e.ShortNameOfInstitution,
                        Discriminator = e.Discriminator,
                        ParentId = e.ParentId,
                    })
                    .ToList()
            })
            .OrderBy(i => i.Id)
            .ToList();
    }
}
