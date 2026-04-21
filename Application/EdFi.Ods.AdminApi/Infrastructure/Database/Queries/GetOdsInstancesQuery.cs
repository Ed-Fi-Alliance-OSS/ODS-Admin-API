// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;
using IUsersContext = EdFi.Admin.DataAccess.Contexts.IUsersContext;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetOdsInstancesQuery
{
    List<OdsInstance> Execute();

    List<OdsInstance> Execute(CommonQueryParams commonQueryParams, int? id, string? name, string? instanceType);
}

public class GetOdsInstancesQuery(IUsersContext userContext, IOptions<AppSettings> options)
    : GetOdsInstancesQueryCore(userContext, options), IGetOdsInstancesQuery
{
}
