// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public interface IGetApiClientDataStoreQuery
{
    ApiClientOdsInstance? Execute(int apiClientId, int dataStoreId);
}

public class GetApiClientDataStoreQuery : IGetApiClientDataStoreQuery
{
    private readonly IUsersContext _usersContext;

    public GetApiClientDataStoreQuery(IUsersContext userContext)
    {
        _usersContext = userContext;
    }
    public ApiClientOdsInstance? Execute(int apiClientId, int dataStoreId)
    {
        var result = _usersContext.ApiClientOdsInstances
            .SingleOrDefault(dataStore => dataStore.ApiClient.ApiClientId == apiClientId && dataStore.OdsInstance.OdsInstanceId == dataStoreId);
        return result;
    }
}



