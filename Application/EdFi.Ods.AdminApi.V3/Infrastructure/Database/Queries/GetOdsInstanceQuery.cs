// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public interface IGetDataStoreQuery
{
    OdsInstance Execute(int id);
}

public class GetDataStoreQuery(IUsersContext userContext) : IGetDataStoreQuery
{
    private readonly IUsersContext _usersContext = userContext;

    public OdsInstance Execute(int id)
    {
        return _usersContext.OdsInstances
            .Include(p => p.OdsInstanceContexts)
            .Include(p => p.OdsInstanceDerivatives)
            .SingleOrDefault(o => o.OdsInstanceId == id)
            ?? throw new NotFoundException<int>("dataStore", id);
    }
}



