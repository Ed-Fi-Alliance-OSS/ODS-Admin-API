// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public class GetApiClientByIdQuery : IGetApiClientByIdQuery
{
    private readonly IUsersContext _context;

    public GetApiClientByIdQuery(IUsersContext context)
    {
        _context = context;
    }

    public ApiClient Execute(int apiClientId)
    {
        var apiClient = _context.ApiClients
            .Include(a => a.Application)
            .SingleOrDefault(app => app.ApiClientId == apiClientId);

        if (apiClient == null)
        {
            throw new NotFoundException<int>("apiclient", apiClientId);
        }

        return apiClient;
    }
}
