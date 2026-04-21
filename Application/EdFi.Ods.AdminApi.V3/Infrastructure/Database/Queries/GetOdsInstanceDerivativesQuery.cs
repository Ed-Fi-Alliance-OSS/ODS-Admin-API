// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public interface IGetOdsInstanceDerivativesQuery
{
    List<OdsInstanceDerivative> Execute();
    List<OdsInstanceDerivative> Execute(CommonQueryParams commonQueryParams);
}

public class GetOdsInstanceDerivativesQuery(IUsersContext usersContext, IOptions<AppSettings> options)
    : GetOdsInstanceDerivativesQueryBase(usersContext, options), IGetOdsInstanceDerivativesQuery
{
    public List<OdsInstanceDerivative> Execute()
    {
        return ExecuteCore();
    }

    public List<OdsInstanceDerivative> Execute(CommonQueryParams commonQueryParams)
    {
        return ExecuteCore(commonQueryParams);
    }
}




