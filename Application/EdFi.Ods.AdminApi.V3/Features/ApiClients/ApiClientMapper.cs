// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

namespace EdFi.Ods.AdminApi.V3.Features.ApiClients;

public static class ApiClientMapper
{
    public static ApiClientResult ToResult(AddApiClientResult source)
    {
        return new ApiClientResult
        {
            Id = source.Id,
            Name = source.Name,
            Key = source.Key,
            Secret = source.Secret,
            ApplicationId = source.ApplicationId
        };
    }

    public static ApiClientResult ToResult(RegenerateApiClientSecretResult source)
    {
        return new ApiClientResult
        {
            Id = source.Id,
            ApplicationId = source.Application.ApplicationId,
            Key = source.Key ?? string.Empty,
            Secret = source.Secret ?? string.Empty
        };
    }

    public static ApiClientModel ToModel(ApiClient source, IList<int> odsInstanceIds)
    {
        return new ApiClientModel
        {
            Id = source.ApiClientId,
            Name = source.Name,
            ClientId = source.Key,
            ApplicationId = source.Application?.ApplicationId ?? 0,
            KeyStatus = source.KeyStatus,
            IsApproved = source.IsApproved,
            EducationOrganizationIds = source.ApplicationEducationOrganizations
                .Select(eu => eu.EducationOrganizationId)
                .ToList(),
            OdsInstanceIds = odsInstanceIds
        };
    }

    public static List<ApiClientModel> ToModelList(
        IEnumerable<ApiClient> source,
        IReadOnlyDictionary<int, IList<int>> odsInstanceIdsByApiClientId)
    {
        return source.Select(apiClient =>
        {
            odsInstanceIdsByApiClientId.TryGetValue(apiClient.ApiClientId, out var odsInstanceIds);
            return ToModel(apiClient, odsInstanceIds ?? new List<int>());
        }).ToList();
    }
}


