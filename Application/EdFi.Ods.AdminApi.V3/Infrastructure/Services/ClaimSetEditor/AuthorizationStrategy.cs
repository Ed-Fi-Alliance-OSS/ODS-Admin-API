// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;

[SwaggerSchema(Title = "ResourceClaimAuthorizationStrategy")]
public class AuthorizationStrategy
{
    // Used internally by AuthStrategyResolver/GetResourcesByClaimSetIdQuery to resolve and
    // compare authorization strategy rows. Hidden from the v3 JSON payload per the v3 API
    // design (only authStrategyName is part of the public contract), but kept on this class
    // (rather than removed) because response DTOs reference this exact object instance.
    [JsonIgnore]
    public int AuthStrategyId { get; set; }

    public string? AuthStrategyName { get; set; }

    [JsonIgnore]
    public bool IsInheritedFromParent { get; set; }
}

