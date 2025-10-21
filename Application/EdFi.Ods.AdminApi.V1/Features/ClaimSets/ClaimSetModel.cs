// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V1.Features.ClaimSets;

[SwaggerSchema(Title = "ClaimSet")]
public class ClaimSetModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsSystemReserved { get; set; }
    public int ApplicationsCount { get; set; }
}

[SwaggerSchema(Title = "ClaimSetWithResources")]
public class ClaimSetDetailsModel : ClaimSetModel
{
    public List<ResourceClaimModel> ResourceClaims { get; set; } = new();
}

[SwaggerSchema(Title = "ResourceClaim")]
public class ResourceClaimModel
{
    public string? Name { get; set; }
    public bool Create { get; set; }
    public bool Read { get; set; }
    public bool Update { get; set; }
    public bool Delete { get; set; }
    public bool ReadChanges { get; set; }
    public AuthorizationStrategiesModel?[] DefaultAuthStrategiesForCRUD { get; set; }
    public AuthorizationStrategiesModel?[] AuthStrategyOverridesForCRUD { get; set; }

    [SwaggerSchema(Description = "Children are collection of ResourceClaim")]
    public List<ResourceClaimModel> Children { get; set; }
    public ResourceClaimModel()
    {
        Children = new List<ResourceClaimModel>();
        DefaultAuthStrategiesForCRUD = Array.Empty<AuthorizationStrategiesModel>();
        AuthStrategyOverridesForCRUD = Array.Empty<AuthorizationStrategiesModel>();
    }
}

[SwaggerSchema(Title = "ResourceClaim")]
public class RequestResourceClaimModel
{
    public string? Name { get; set; }
    public bool Create { get; set; }
    public bool Read { get; set; }
    public bool Update { get; set; }
    public bool Delete { get; set; }
    public bool ReadChanges { get; set; }
    public AuthorizationStrategiesModel?[] AuthStrategyOverridesForCRUD { get; set; }

    [SwaggerSchema(Description = "Children are collection of ResourceClaim")]
    public List<ChildrenRequestResourceClaimModel> Children { get; set; }
    public RequestResourceClaimModel()
    {
        Children = new List<ChildrenRequestResourceClaimModel>();
        AuthStrategyOverridesForCRUD = Array.Empty<AuthorizationStrategiesModel>();
    }
}

public class ChildrenRequestResourceClaimModel : RequestResourceClaimModel
{
    public new List<RequestResourceClaimModel> Children { get; set; } = new List<RequestResourceClaimModel>();
}

[SwaggerSchema(Title = "AuthorizationStrategies")]
public class AuthorizationStrategiesModel
{
    public AuthorizationStrategyModel?[] AuthorizationStrategies { get; set; }
    public AuthorizationStrategiesModel()
    {
        AuthorizationStrategies = Array.Empty<AuthorizationStrategyModel>();

    }
}

[SwaggerSchema(Title = "AuthorizationStrategy")]
public class AuthorizationStrategyModel
{
    //[SwaggerExclude]
    public int AuthStrategyId { get; set; }

    public string? AuthStrategyName { get; set; }

    //[SwaggerExclude]
    public string? DisplayName { get; set; }

    public bool IsInheritedFromParent { get; set; }
}

public class EditClaimSetModel : IEditClaimSetModel
{
    public string? ClaimSetName { get; set; }

    public int ClaimSetId { get; set; }
}

public class UpdateResourcesOnClaimSetModel : IUpdateResourcesOnClaimSetModel
{
    public int ClaimSetId { get; set; }

    public List<ResourceClaim>? ResourceClaims { get; set; } = new List<ResourceClaim>();
}

public class DeleteClaimSetModel : IDeleteClaimSetModel
{
    public string? Name { get; set; }

    public int Id { get; set; }
}
