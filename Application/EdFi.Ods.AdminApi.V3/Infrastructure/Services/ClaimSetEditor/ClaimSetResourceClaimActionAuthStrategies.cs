// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;

public interface IClaimSetResourceClaimActionAuthStrategies
{
    int? ActionId { get; }
    string? ActionName { get; }
    IEnumerable<AuthorizationStrategy>? AuthorizationStrategies { get; }
}

[SwaggerSchema(Title = "ClaimSetResourceClaimActionAuthorizationStrategies")]
public class ClaimSetResourceClaimActionAuthStrategies : IClaimSetResourceClaimActionAuthStrategies
{
    public int? ActionId { get; set; }
    public string? ActionName { get; set; }
    public IEnumerable<AuthorizationStrategy>? AuthorizationStrategies { get; set; }
}
