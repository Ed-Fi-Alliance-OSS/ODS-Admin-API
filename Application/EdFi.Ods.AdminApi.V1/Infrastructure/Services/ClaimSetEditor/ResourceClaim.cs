// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using Newtonsoft.Json;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;

public class ResourceClaim
{
    public int Id { get; set; }
    public int ParentId { get; set; }
    public string? ParentName { get; set; }
    public string? Name { get; set; }
    public bool Create { get; set; }
    public bool Read { get; set; }
    public bool Update { get; set; }
    public bool Delete { get; set; }
    public bool ReadChanges { get; set; }
    [JsonIgnore]
    public bool IsParent { get; set; }
    public ClaimSetResourceClaimActionAuthStrategies?[] DefaultAuthStrategiesForCRUD { get; set; } = Array.Empty<ClaimSetResourceClaimActionAuthStrategies>();
    public ClaimSetResourceClaimActionAuthStrategies?[] AuthStrategyOverridesForCRUD { get; set; } = Array.Empty<ClaimSetResourceClaimActionAuthStrategies>();
    public List<ResourceClaim> Children { get; set; } = new();

    public ResourceClaim()
    {
        Children = new List<ResourceClaim>();
    }
}
