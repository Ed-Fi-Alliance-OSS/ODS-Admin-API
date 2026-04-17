// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V2.Features.ClaimSets;
using EdFi.Ods.AdminApi.V2.Infrastructure.ClaimSetEditor;

namespace EdFi.Ods.AdminApi.V2.Features.ResourceClaims;

public static class ResourceClaimMapper
{
    public static ResourceClaimModel ToModel(ResourceClaim source)
    {
        return new ResourceClaimModel
        {
            Id = source.Id,
            Name = source.Name,
            ParentId = source.ParentId,
            ParentName = source.ParentName,
            Children = ToModelList(source.Children)
        };
    }

    public static List<ResourceClaimModel> ToModelList(IEnumerable<ResourceClaim> source)
    {
        return source.Select(ToModel).ToList();
    }
}
