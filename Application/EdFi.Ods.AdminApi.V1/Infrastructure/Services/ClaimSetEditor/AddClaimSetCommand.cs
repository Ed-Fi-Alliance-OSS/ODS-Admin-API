// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;

public class AddClaimSetCommand
{
    private readonly AddClaimSetCommandService _service;

    public AddClaimSetCommand(AddClaimSetCommandService service)
    {
        _service = service;
    }

    public int Execute(IAddClaimSetModel claimSet)
    {
        return _service.Execute(claimSet);
    }
}

public interface IAddClaimSetModel
{
    string ClaimSetName { get; }
}
