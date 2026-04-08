// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.Features.OdsInstances;

public static class EducationOrganizationMapper
{
    public static EducationOrganizationModel ToModel(EducationOrganization source)
    {
        return new EducationOrganizationModel
        {
            EducationOrganizationId = source.EducationOrganizationId,
            NameOfInstitution = source.NameOfInstitution,
            ShortNameOfInstitution = source.ShortNameOfInstitution,
            Discriminator = source.Discriminator,
            ParentId = source.ParentId,
        };
    }

    public static List<EducationOrganizationModel> ToModelList(IEnumerable<EducationOrganization> source)
    {
        return source.Select(ToModel).ToList();
    }
}