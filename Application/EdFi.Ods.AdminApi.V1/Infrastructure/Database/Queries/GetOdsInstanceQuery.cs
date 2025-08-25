// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;

public interface IGetOdsInstanceQuery
{
    OdsInstance Execute(int odsInstanceId);
}

public class GetOdsInstanceQuery : IGetOdsInstanceQuery
{
    private readonly IUsersContext _usersContext;

    public GetOdsInstanceQuery(IUsersContext userContext)
    {
        _usersContext = userContext;
    }

    public OdsInstance Execute(int odsInstanceId)
    {
        return _usersContext.OdsInstances
            .SingleOrDefault(odsInstance => odsInstance.OdsInstanceId == odsInstanceId) ?? throw new NotFoundException<int>("odsInstance", odsInstanceId);
    }
}
