// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;

public class GetClaimSetByIdQuery : IGetClaimSetByIdQuery
{
    private readonly GetClaimSetByIdQueryService _service;

    public GetClaimSetByIdQuery(GetClaimSetByIdQueryService service)
    {
        _service = service;
    }

    public ClaimSet Execute(int securityContextClaimSetId)
    {
        return _service.Execute(securityContextClaimSetId);
    }
}

public interface IGetClaimSetByIdQuery
{
    ClaimSet Execute(int securityContextClaimSetId);
}

