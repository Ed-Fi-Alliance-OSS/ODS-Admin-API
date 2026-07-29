// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public interface IGetDataStoreManageByIdQuery
{
    OdsInstanceManage? Execute(int id);
}

public class GetDataStoreManageByIdQuery : IGetDataStoreManageByIdQuery
{
    private readonly AdminApiDbContext _context;

    public GetDataStoreManageByIdQuery(AdminApiDbContext context)
    {
        _context = context;
    }

    public OdsInstanceManage? Execute(int id)
    {
        return _context.OdsInstanceManages.SingleOrDefault(d => d.Id == id);
    }
}
