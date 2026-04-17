// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V2.Infrastructure.ClaimSetEditor;

[SwaggerSchema(Title = "ResourceClaimAuthorizationStrategy")]
public class AuthorizationStrategy
{
    public int AuthStrategyId { get; set; }
    public string? AuthStrategyName { get; set; }
    public bool IsInheritedFromParent { get; set; }
}
