// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;

public interface IClaimSetResourceClaimActionAuthStrategies
{
    IList<AuthorizationStrategy?> AuthorizationStrategies { get; }
}

[SwaggerSchema(Title = "ClaimSetResourceClaimActionAuthorizationStrategies")]
public class ClaimSetResourceClaimActionAuthStrategies : IClaimSetResourceClaimActionAuthStrategies
{
    public IList<AuthorizationStrategy?> AuthorizationStrategies { get; set; } = new List<AuthorizationStrategy?>();
}
