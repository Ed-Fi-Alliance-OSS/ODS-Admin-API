// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetDbInstanceByIdQuery
{
    DbInstance? Execute(int id);
}

public class GetDbInstanceByIdQuery : IGetDbInstanceByIdQuery
{
    private readonly EdFi.Ods.AdminApi.Common.Infrastructure.AdminApiDbContext _context;

    public GetDbInstanceByIdQuery(EdFi.Ods.AdminApi.Common.Infrastructure.AdminApiDbContext context)
    {
        _context = context;
    }

    public DbInstance? Execute(int id)
    {
        return _context.DbInstances.SingleOrDefault(d => d.Id == id);
    }
}
