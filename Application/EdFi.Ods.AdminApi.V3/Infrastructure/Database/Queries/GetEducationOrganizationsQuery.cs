// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features.DataStores;
using EdFi.Ods.AdminApi.V3.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.V3.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public interface IGetEducationOrganizationsQuery
{
    Task<List<DataStoreWithEducationOrganizationsModel>> ExecuteAsync();

    Task<List<DataStoreWithEducationOrganizationsModel>> ExecuteAsync(CommonQueryParams commonQueryParams, int? dataStoreId);
}

public class GetEducationOrganizationsQuery : IGetEducationOrganizationsQuery
{
    private readonly AdminApiDbContext _adminApiDbContext;
    private readonly IOptions<AppSettings> _options;
    private readonly Dictionary<string, Expression<Func<EducationOrganization, object>>> _orderByColumns;

    public GetEducationOrganizationsQuery(
        AdminApiDbContext adminApiDbContext,
        IOptions<AppSettings> options)
    {
        _adminApiDbContext = adminApiDbContext;
        _options = options;

        var isSQLServerEngine = _options.Value.DatabaseEngine?.ToLowerInvariant() == DatabaseEngineEnum.SqlServer.ToLowerInvariant();

        _orderByColumns = new Dictionary<string, Expression<Func<EducationOrganization, object>>>
            (StringComparer.OrdinalIgnoreCase)
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

    public async Task<List<DataStoreWithEducationOrganizationsModel>> ExecuteAsync()
    {
        var educationOrganizations = await _adminApiDbContext.EducationOrganizations
            .OrderBy(e => e.InstanceId)
            .ThenBy(e => e.EducationOrganizationId)
            .ToListAsync();

        return GroupEducationOrganizationsByInstance(educationOrganizations);
    }

    public async Task<List<DataStoreWithEducationOrganizationsModel>> ExecuteAsync(
        CommonQueryParams commonQueryParams,
        int? dataStoreId)
    {
        Expression<Func<EducationOrganization, object>> columnToOrderBy =
            _orderByColumns.GetColumnToOrderBy(commonQueryParams.OrderBy);

        var educationOrganizations = await _adminApiDbContext.EducationOrganizations
            .Where(e => dataStoreId == null || e.InstanceId == dataStoreId)
            .OrderByColumn(columnToOrderBy, commonQueryParams.IsDescending)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToListAsync();

        return GroupEducationOrganizationsByInstance(educationOrganizations);
    }

    private List<DataStoreWithEducationOrganizationsModel> GroupEducationOrganizationsByInstance(
        List<EducationOrganization> educationOrganizations)
    {
        return educationOrganizations
            .GroupBy(e => new { e.InstanceId, e.InstanceName })
            .Select(group => new DataStoreWithEducationOrganizationsModel
            {
                Id = group.Key.InstanceId,
                Name = group.Key.InstanceName ?? string.Empty,
                DataStoreType = null,
                EducationOrganizations = EducationOrganizationMapper.ToModelList(group.ToList())
            })
            .OrderBy(i => i.Id)
            .ToList();
    }
}



