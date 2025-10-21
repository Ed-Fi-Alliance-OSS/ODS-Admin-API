// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using EdFi.Ods.AdminApi.V1.Security.DataAccess.Contexts;
using Microsoft.EntityFrameworkCore;


namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor
{
    public class UpdateResourcesOnClaimSetCommandService
    {
        private readonly ISecurityContext _context;
        private readonly AddOrEditResourcesOnClaimSetCommand _addOrEditResourcesOnClaimSetCommand;

        public UpdateResourcesOnClaimSetCommandService(ISecurityContext context,
            AddOrEditResourcesOnClaimSetCommand addOrEditResourcesOnClaimSetCommand)
        {
            _context = context;
            _addOrEditResourcesOnClaimSetCommand = addOrEditResourcesOnClaimSetCommand;
        }

        public void Execute(IUpdateResourcesOnClaimSetModel model)
        {
            var resourceClaimsForClaimSet =
                _context
                .ClaimSetResourceClaimActions
                .Include(x => x.AuthorizationStrategyOverrides!).ThenInclude(x => x.AuthorizationStrategy)
                .Where(x => x.ClaimSet.ClaimSetId == model.ClaimSetId).ToList();
            _context.ClaimSetResourceClaimActions.RemoveRange(resourceClaimsForClaimSet);
            _context.SaveChanges();

            if (model.ResourceClaims == null)
                return;

            _addOrEditResourcesOnClaimSetCommand.Execute(model.ClaimSetId, model.ResourceClaims);
        }
    }
}
