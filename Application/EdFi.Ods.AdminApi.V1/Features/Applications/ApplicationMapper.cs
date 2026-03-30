// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V1.Infrastructure;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Commands;

namespace EdFi.Ods.AdminApi.V1.Features.Applications;

public static class ApplicationMapper
{
    public static ApplicationModel ToModel(Admin.DataAccess.Models.Application source)
    {
        return new ApplicationModel
        {
            ApplicationId = source.ApplicationId,
            ApplicationName = source.ApplicationName,
            ClaimSetName = source.ClaimSetName,
            ProfileName = source.ProfileName(),
            EducationOrganizationIds = source.EducationOrganizationIds(),
            OdsInstanceId = source.OdsInstance != null ? source.OdsInstance.OdsInstanceId : 0,
            OdsInstanceName = source.OdsInstanceName(),
            VendorId = source.VendorId(),
            Profiles = source.Profiles(),
        };
    }

    public static List<ApplicationModel> ToModelList(IEnumerable<Admin.DataAccess.Models.Application> source)
    {
        return source.Select(ToModel).ToList();
    }

    public static ApplicationResult ToResult(AddApplicationResult source)
    {
        return new ApplicationResult
        {
            ApplicationId = source.ApplicationId,
            Key = source.Key,
            Secret = source.Secret,
        };
    }

    public static ApplicationResult ToResult(RegenerateApiClientSecretResult source)
    {
        return new ApplicationResult
        {
            ApplicationId = source.Application.ApplicationId,
            Key = source.Key,
            Secret = source.Secret,
        };
    }
}