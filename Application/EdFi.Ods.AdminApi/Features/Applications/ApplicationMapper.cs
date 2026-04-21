// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;

namespace EdFi.Ods.AdminApi.Features.Applications;

public static class ApplicationMapper
{
    public static ApplicationResult ToResult(AddApplicationResult source)
    {
        return new ApplicationResult
        {
            Id = source.ApplicationId,
            Key = source.Key,
            Secret = source.Secret
        };
    }

    public static ApplicationResult ToResult(RegenerateApplicationApiClientSecretResult source)
    {
        return new ApplicationResult
        {
            Id = source.Application.ApplicationId,
            Key = source.Key,
            Secret = source.Secret
        };
    }

    public static ApplicationModel ToModel(Application source, IList<int> odsInstanceIds)
    {
        return new ApplicationModel
        {
            Id = source.ApplicationId,
            ApplicationName = source.ApplicationName,
            ClaimSetName = source.ClaimSetName,
            EducationOrganizationIds = source.EducationOrganizationIds(),
            VendorId = source.VendorId(),
            ProfileIds = source.Profiles(),
            Enabled = source.ApiClients.All(a => a.IsApproved),
            OdsInstanceIds = odsInstanceIds
        };
    }

    public static List<ApplicationModel> ToModelList(
        IEnumerable<Application> source,
        IReadOnlyDictionary<int, IList<int>> odsInstanceIdsByApplicationId)
    {
        return source
            .Select(application =>
            {
                odsInstanceIdsByApplicationId.TryGetValue(application.ApplicationId, out var odsInstanceIds);
                return ToModel(application, odsInstanceIds ?? new List<int>());
            })
            .ToList();
    }
}
