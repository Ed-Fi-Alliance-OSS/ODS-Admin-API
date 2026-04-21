// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq.Expressions;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;


public interface IGetOdsInstanceContextsQuery
{
    List<OdsInstanceContext> Execute();
    List<OdsInstanceContext> Execute(CommonQueryParams commonQueryParams);
}

public class GetOdsInstanceContextsQuery(IUsersContext usersContext, IOptions<AppSettings> options)
    : GetOdsInstanceContextsQueryBase(usersContext, options), IGetOdsInstanceContextsQuery
{
    public List<OdsInstanceContext> Execute()
    {
        return ExecuteCore();
    }

    public List<OdsInstanceContext> Execute(CommonQueryParams commonQueryParams)
    {
        return ExecuteCore(commonQueryParams);
    }
}

