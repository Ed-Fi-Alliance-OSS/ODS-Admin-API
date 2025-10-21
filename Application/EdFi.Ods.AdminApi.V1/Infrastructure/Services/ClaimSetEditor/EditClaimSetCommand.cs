// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor
{
    public interface IEditClaimSetCommand
    {
        int Execute(IEditClaimSetModel claimSet);
    }

    public class EditClaimSetCommand : IEditClaimSetCommand
    {
        private readonly EditClaimSetCommandService _service;

        public EditClaimSetCommand(EditClaimSetCommandService service)
        {
            _service = service;
        }

        public int Execute(IEditClaimSetModel claimSet)
        {
            return _service.Execute(claimSet);
        }
    }

    public interface IEditClaimSetModel
    {
        string? ClaimSetName { get; }
        int ClaimSetId { get; }
    }
}
