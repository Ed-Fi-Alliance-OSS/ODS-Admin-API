// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetApplicationByNameAndClaimsetQuery
{
    Application? Execute(string applicationName, string claimset);
}

public class GetApplicationByNameAndClaimsetQuery : IGetApplicationByNameAndClaimsetQuery
{
    private readonly IUsersContext _context;

    public GetApplicationByNameAndClaimsetQuery(IUsersContext context)
    {
        _context = context;
    }

    public Application? Execute(string applicationName, string claimset)
    {
        var application = _context.Applications
            .Include(a => a.ApplicationEducationOrganizations)
            .Include(a => a.Profiles)
            .Include(a => a.Vendor)
            .SingleOrDefault(app => app.ApplicationName == applicationName && app.ClaimSetName == claimset);
        return application;
    }
}
