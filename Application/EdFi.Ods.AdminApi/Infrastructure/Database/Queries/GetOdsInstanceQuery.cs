// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetOdsInstanceQuery
{
    OdsInstance Execute(int odsInstanceId);
}

public class GetOdsInstanceQuery(IUsersContext userContext) : IGetOdsInstanceQuery
{
    private readonly IUsersContext _usersContext = userContext;

    public OdsInstance Execute(int odsInstanceId)
    {
        return _usersContext.OdsInstances
            .Include(p => p.OdsInstanceContexts)
            .Include(p => p.OdsInstanceDerivatives)
            .SingleOrDefault(odsInstance => odsInstance.OdsInstanceId == odsInstanceId) ?? throw new NotFoundException<int>("odsInstance", odsInstanceId);
    }
}
