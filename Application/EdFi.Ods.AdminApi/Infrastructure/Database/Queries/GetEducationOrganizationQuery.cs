// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.Infrastructure.Database.Queries;

public interface IGetEducationOrganizationQuery
{
    List<EducationOrganization> Execute();

    List<EducationOrganization> Execute(int odsInstanceId);

    List<EducationOrganization> Execute(int[] odsInstanceIds);
}

public class GetEducationOrganizationQuery(EdFi.Ods.AdminApi.Common.Infrastructure.AdminApiDbContext adminApiDbContext)
    : GetEducationOrganizationQueryBase(adminApiDbContext), IGetEducationOrganizationQuery
{
    public List<EducationOrganization> Execute() => ExecuteCore();

    public List<EducationOrganization> Execute(int odsInstanceId) => ExecuteCore(odsInstanceId);

    public List<EducationOrganization> Execute(int[] odsInstanceIds) => ExecuteCore(odsInstanceIds);
}
