// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;

public interface IGetEducationOrganizationQuery
{
    List<EducationOrganization> Execute();

    List<EducationOrganization> Execute(int dataStoreId);

    List<EducationOrganization> Execute(int[] dataStoreIds);
}

public class GetEducationOrganizationQuery(AdminApiDbContext adminApiDbContext) : IGetEducationOrganizationQuery
{
    private readonly AdminApiDbContext _adminApiDbContext = adminApiDbContext;

    public List<EducationOrganization> Execute()
    {
        return _adminApiDbContext.EducationOrganizations.ToList();
    }

    public List<EducationOrganization> Execute(int dataStoreId)
    {
        return _adminApiDbContext.EducationOrganizations
                .Where(edOrgs => edOrgs.InstanceId == dataStoreId)
                .ToList();
    }

    public List<EducationOrganization> Execute(int[] dataStoreIds)
    {
        return _adminApiDbContext.EducationOrganizations
                .Where(edOrgs => dataStoreIds.Contains(edOrgs.InstanceId))
                .ToList();
    }
}



