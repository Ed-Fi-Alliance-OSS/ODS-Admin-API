// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public interface IGetDbInstanceByIdQuery
{
    DbInstance? Execute(int id);
}

public class GetDbInstanceByIdQuery(EdFi.Ods.AdminApi.Common.Infrastructure.AdminApiDbContext context)
    : GetDbInstanceByIdQueryBase(context), IGetDbInstanceByIdQuery
{
    public DbInstance? Execute(int id)
    {
        return ExecuteCore(id);
    }
}



