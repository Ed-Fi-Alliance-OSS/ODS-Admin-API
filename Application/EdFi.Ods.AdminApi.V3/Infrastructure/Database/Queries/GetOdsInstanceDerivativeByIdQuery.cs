// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public interface IGetDataStoreDerivativeByIdQuery
{
    OdsInstanceDerivative Execute(int dataStoreDerivativeId);
}

public class GetDataStoreDerivativeByIdQuery : IGetDataStoreDerivativeByIdQuery
{
    private readonly IUsersContext _context;

    public GetDataStoreDerivativeByIdQuery(IUsersContext context)
    {
        _context = context;
    }

    public OdsInstanceDerivative Execute(int dataStoreDerivativeId)
    {
        var odsInstanceDerivative = _context.OdsInstanceDerivatives
            .Include(oid => oid.OdsInstance)
            .SingleOrDefault(app => app.OdsInstanceDerivativeId == dataStoreDerivativeId);
        if (odsInstanceDerivative == null)
        {
            throw new NotFoundException<int>("dataStoreDerivative", dataStoreDerivativeId);
        }

        return odsInstanceDerivative;
    }
}



