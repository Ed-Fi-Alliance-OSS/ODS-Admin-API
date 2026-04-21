// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Security.DataAccess.Contexts;
using Microsoft.Extensions.Options;
using Action = EdFi.Security.DataAccess.Models.Action;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetAllActionsQuery
{
    IReadOnlyList<Action> Execute();
    IReadOnlyList<Action> Execute(CommonQueryParams commonQueryParams, int? id, string? name);
}

public class GetAllActionsQuery(ISecurityContext securityContext, IOptions<AppSettings> options)
    : GetAllActionsQueryCore(securityContext, options), IGetAllActionsQuery
{
}
