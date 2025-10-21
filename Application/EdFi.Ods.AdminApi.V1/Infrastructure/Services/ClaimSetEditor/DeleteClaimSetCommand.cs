// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;

public interface IDeleteClaimSetCommand
{
    void Execute(IDeleteClaimSetModel claimSet);
}

public class DeleteClaimSetCommand : IDeleteClaimSetCommand
{
    private readonly DeleteClaimSetCommandService _service;

    public DeleteClaimSetCommand(DeleteClaimSetCommandService service)
    {
        _service = service;
    }

    public void Execute(IDeleteClaimSetModel claimSet)
    {
        _service.Execute(claimSet);
    }
}

public interface IDeleteClaimSetModel
{
    string? Name { get; }
    int Id { get; }
}
