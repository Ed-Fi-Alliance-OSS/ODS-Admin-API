// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public abstract class GetEducationOrganizationQueryBase(AdminApiDbContext adminApiDbContext)
{
    private readonly AdminApiDbContext _adminApiDbContext = adminApiDbContext;

    protected List<EducationOrganization> ExecuteCore()
    {
        return _adminApiDbContext.EducationOrganizations.ToList();
    }

    protected List<EducationOrganization> ExecuteCore(int odsInstanceId)
    {
        return _adminApiDbContext.EducationOrganizations
            .Where(edOrgs => edOrgs.InstanceId == odsInstanceId)
            .ToList();
    }

    protected List<EducationOrganization> ExecuteCore(int[] odsInstanceIds)
    {
        return _adminApiDbContext.EducationOrganizations
            .Where(edOrgs => odsInstanceIds.Contains(edOrgs.InstanceId))
            .ToList();
    }
}
