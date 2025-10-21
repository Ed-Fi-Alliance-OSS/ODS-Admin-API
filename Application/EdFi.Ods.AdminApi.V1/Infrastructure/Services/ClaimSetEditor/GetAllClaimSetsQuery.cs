// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;

public interface IGetAllClaimSetsQuery
{
    IReadOnlyList<ClaimSet> Execute();
    IReadOnlyList<ClaimSet> Execute(CommonQueryParams commonQueryParams);
}

public class GetAllClaimSetsQuery : IGetAllClaimSetsQuery
{
    private readonly GetAllClaimSetsQueryService _service;

    public GetAllClaimSetsQuery(GetAllClaimSetsQueryService service) => _service = service;

    public IReadOnlyList<ClaimSet> Execute()
    {
        return _service.Execute();
    }

    public IReadOnlyList<ClaimSet> Execute(CommonQueryParams commonQueryParams)
    {
        return _service.Execute(commonQueryParams);
    }
}
