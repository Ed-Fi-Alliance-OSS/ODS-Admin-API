// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V1.Infrastructure.Extensions;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;

public interface IGetOdsInstancesQuery
{
    List<OdsInstance> Execute();

    List<OdsInstance> Execute(CommonQueryParams commonQueryParams);
}

public class GetOdsInstancesQuery : IGetOdsInstancesQuery
{
    private readonly IUsersContext _usersContext;
    private readonly IOptions<AppSettings> _options;

    public GetOdsInstancesQuery(IUsersContext userContext, IOptions<AppSettings> options)
    {
        _usersContext = userContext;
        _options = options;
    }

    public List<OdsInstance> Execute()
    {
        return _usersContext.OdsInstances.OrderBy(odsInstance => odsInstance.Name).ToList();
    }

    public List<OdsInstance> Execute(CommonQueryParams commonQueryParams)
    {
        return _usersContext.OdsInstances
            .OrderBy(o => o.Name)
            .Paginate(commonQueryParams.Offset, commonQueryParams.Limit, _options)
            .ToList();
    }
}
