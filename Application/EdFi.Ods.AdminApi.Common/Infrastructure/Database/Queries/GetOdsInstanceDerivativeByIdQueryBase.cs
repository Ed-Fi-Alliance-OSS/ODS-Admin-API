// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetOdsInstanceDerivativeByIdQueryBase(IUsersContext context)
{
    private readonly IUsersContext _context = context;

    protected OdsInstanceDerivative ExecuteCore(int odsInstanceDerivativeId)
    {
        var odsInstanceDerivative = _context.OdsInstanceDerivatives
            .Include(oid => oid.OdsInstance)
            .SingleOrDefault(app => app.OdsInstanceDerivativeId == odsInstanceDerivativeId);

        if (odsInstanceDerivative == null)
        {
            throw new NotFoundException<int>("odsInstanceDerivative", odsInstanceDerivativeId);
        }

        return odsInstanceDerivative;
    }
}
