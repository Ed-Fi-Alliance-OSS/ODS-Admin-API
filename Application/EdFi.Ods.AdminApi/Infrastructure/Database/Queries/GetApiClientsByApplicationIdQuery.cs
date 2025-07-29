// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Common.Extensions;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public class GetApiClientsByApplicationIdQuery : IGetApiClientsByApplicationIdQuery
{
    private readonly IUsersContext _context;

    public GetApiClientsByApplicationIdQuery(IUsersContext context)
    {
        _context = context;
    }

    public IReadOnlyList<ApiClient> Execute(int applicationId)
    {
        var apiClients = _context.ApiClients
            .Include(ac => ac.Application)
            .Include(ac => ac.User)
            .Where(app => applicationId == 0 || app.Application.ApplicationId == applicationId);

        return apiClients.ToReadOnlyList();
    }
}
