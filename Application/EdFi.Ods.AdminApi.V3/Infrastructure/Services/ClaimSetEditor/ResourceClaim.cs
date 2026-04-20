// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor
{
    public class ResourceClaim
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public string? ParentName { get; set; }
        public string? Name { get; set; }
        public List<ResourceClaimAction>? Actions { get; set; }
        [JsonIgnore]
        public bool IsParent { get; set; }
        public List<ClaimSetResourceClaimActionAuthStrategies?> DefaultAuthorizationStrategiesForCRUD { get; set; } = new List<ClaimSetResourceClaimActionAuthStrategies?>();
        public List<ClaimSetResourceClaimActionAuthStrategies?> AuthorizationStrategyOverridesForCRUD { get; set; } = new List<ClaimSetResourceClaimActionAuthStrategies?>();
        public List<ResourceClaim> Children { get; set; }

        public ResourceClaim()
        {
            Children = new List<ResourceClaim>();
        }
    }

    [SwaggerSchema(Title = "ResourceClaimAction")]
    public class ResourceClaimAction
    {
        public string? Name { get; set; }
        public bool Enabled { get; set; }
    }
}

