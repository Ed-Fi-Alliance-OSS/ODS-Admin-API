// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;

public interface IGetApplicationsByOdsInstanceIdQuery
{
    List<Application> Execute(int odsInstanceId);
}

public class GetApplicationsByOdsInstanceIdQuery : IGetApplicationsByOdsInstanceIdQuery
{
    private readonly IUsersContext _context;

    public GetApplicationsByOdsInstanceIdQuery(IUsersContext context)
    {
        _context = context;
    }

    public List<Application> Execute(int odsInstanceId)
    {
        var applications = _context.Applications
            .Include(aco => aco.OdsInstance)
            .Include(aco => aco.ApplicationEducationOrganizations)
            .Include(api => api.Profiles)
            .Include(api => api.Vendor)
            .Where(a => a.OdsInstance == null || a.OdsInstance.OdsInstanceId == odsInstanceId)
            .ToList();

        if (!applications.Any() && _context.OdsInstances.Find(odsInstanceId) == null)
        {
            throw new NotFoundException<int>("odsinstance", odsInstanceId);
        }

        return applications;
    }
}
