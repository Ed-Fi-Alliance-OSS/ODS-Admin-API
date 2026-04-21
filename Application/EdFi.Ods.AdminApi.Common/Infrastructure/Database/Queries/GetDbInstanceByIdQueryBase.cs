// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetDbInstanceByIdQueryBase(AdminApiDbContext context)
{
    private readonly AdminApiDbContext _context = context;

    protected DbInstance? ExecuteCore(int id)
    {
        return _context.DbInstances.SingleOrDefault(d => d.Id == id);
    }
}