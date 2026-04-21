// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;

public class GetEducationOrganizationQueryCore(AdminApiDbContext adminApiDbContext)
{
    private readonly AdminApiDbContext _adminApiDbContext = adminApiDbContext;

    public List<EducationOrganization> Execute()
    {
        return _adminApiDbContext.EducationOrganizations.ToList();
    }

    public List<EducationOrganization> Execute(int odsInstanceId)
    {
        return _adminApiDbContext.EducationOrganizations
            .Where(edOrgs => edOrgs.InstanceId == odsInstanceId)
            .ToList();
    }

    public List<EducationOrganization> Execute(int[] odsInstanceIds)
    {
        return _adminApiDbContext.EducationOrganizations
            .Where(edOrgs => odsInstanceIds.Contains(edOrgs.InstanceId))
            .ToList();
    }
}
