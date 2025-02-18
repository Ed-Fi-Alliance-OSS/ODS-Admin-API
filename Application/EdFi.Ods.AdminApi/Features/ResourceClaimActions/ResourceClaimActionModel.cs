// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Features.ResourceClaimActions
{
    public class ResourceClaimActionModel
    {
        public int ResourceClaimId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public string ClaimName { get; set; } = string.Empty;
        public List<ActionForResourceClaimModel> Actions { get; set; } = new List<ActionForResourceClaimModel>();
    }

    public class ActionForResourceClaimModel
    {
        public string Name { get; set; } = string.Empty;
    }
}
