// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetApiClientIdByApplicationIdQuery
{
    ApiClient Execute(int applicationId);
}

public class GetApiClientIdByApplicationIdQuery(IUsersContext context)
    : GetApiClientIdByApplicationIdQueryCore(context), IGetApiClientIdByApplicationIdQuery
{
}
