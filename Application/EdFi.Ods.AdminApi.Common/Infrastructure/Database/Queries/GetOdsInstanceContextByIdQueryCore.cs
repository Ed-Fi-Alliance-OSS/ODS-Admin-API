// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public class GetOdsInstanceContextByIdQueryCore(IUsersContext context)
{
    private readonly IUsersContext _context = context;

    public OdsInstanceContext Execute(int odsInstanceContextId)
    {
        var odsInstanceContext = _context.OdsInstanceContexts
            .Include(oid => oid.OdsInstance)
            .SingleOrDefault(app => app.OdsInstanceContextId == odsInstanceContextId);

        if (odsInstanceContext == null)
        {
            throw new NotFoundException<int>("odsInstanceContext", odsInstanceContextId);
        }

        return odsInstanceContext;
    }
}
