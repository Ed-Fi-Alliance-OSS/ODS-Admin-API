// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor
{
    public class UpdateResourcesOnClaimSetCommand
    {
        private readonly UpdateResourcesOnClaimSetCommandService _service;

        public UpdateResourcesOnClaimSetCommand(UpdateResourcesOnClaimSetCommandService service)
        {
            _service = service;
        }

        public void Execute(IUpdateResourcesOnClaimSetModel model)
        {
            _service.Execute(model);
        }
    }

    public interface IUpdateResourcesOnClaimSetModel
    {
        int ClaimSetId { get; }
        List<ResourceClaim>? ResourceClaims { get; }
    }
}
