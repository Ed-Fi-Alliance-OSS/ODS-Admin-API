// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public interface IGetEducationOrganizationsQuery
{
    Task<List<OdsInstanceWithEducationOrganizationsModel>> ExecuteAsync();

    Task<List<OdsInstanceWithEducationOrganizationsModel>> ExecuteAsync(CommonQueryParams commonQueryParams, int? instanceId);
}

public class GetEducationOrganizationsQuery(AdminApiDbContext adminApiDbContext, IOptions<AppSettings> options)
    : GetEducationOrganizationsQueryBase(adminApiDbContext, options), IGetEducationOrganizationsQuery
{
    public Task<List<OdsInstanceWithEducationOrganizationsModel>> ExecuteAsync() => ExecuteCoreAsync();

    public Task<List<OdsInstanceWithEducationOrganizationsModel>> ExecuteAsync(CommonQueryParams commonQueryParams, int? instanceId)
        => ExecuteCoreAsync(commonQueryParams, instanceId);
}

