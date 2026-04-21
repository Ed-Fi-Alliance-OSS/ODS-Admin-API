// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetApiClientOdsInstanceQueryBase(IUsersContext usersContext)
{
    private readonly IUsersContext _usersContext = usersContext;

    protected ApiClientOdsInstance? ExecuteCore(int apiClientId, int odsInstanceId)
    {
        return _usersContext.ApiClientOdsInstances
            .SingleOrDefault(odsInstance => odsInstance.ApiClient.ApiClientId == apiClientId && odsInstance.OdsInstance.OdsInstanceId == odsInstanceId);
    }
}
