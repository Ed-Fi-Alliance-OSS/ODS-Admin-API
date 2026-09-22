// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;

public interface IDeleteResouceClaimOnClaimSetCommand
{
    void Execute(int claimSetId, int resourceClaimId);
}

public class DeleteResouceClaimOnClaimSetCommand : IDeleteResouceClaimOnClaimSetCommand
{
    private readonly ISecurityContext _context;
    private readonly IDeletedEntityAuditCapture _capture;

    public DeleteResouceClaimOnClaimSetCommand(ISecurityContext context, IDeletedEntityAuditCapture capture)
    {
        _context = context;
        _capture = capture;
    }

    public void Execute(int claimSetId, int resourceClaimId)
    {
        var resourceClaimsForClaimSetId =
                  _context.ClaimSetResourceClaimActions.Where(x => x.ClaimSetId == claimSetId && x.ResourceClaimId == resourceClaimId).ToList();

        _capture.Record(resourceClaimsForClaimSetId.FirstOrDefault());

        foreach (var resourceClaimAction in resourceClaimsForClaimSetId)
        {
            var resourceClaimActionAuthorizationStrategyOverrides = _context.ClaimSetResourceClaimActionAuthorizationStrategyOverrides.
                Where(x => x.ClaimSetResourceClaimActionId == resourceClaimAction.ClaimSetResourceClaimActionId);

            _context.ClaimSetResourceClaimActionAuthorizationStrategyOverrides.RemoveRange(resourceClaimActionAuthorizationStrategyOverrides);
        }

        _context.ClaimSetResourceClaimActions.RemoveRange(resourceClaimsForClaimSetId);
        _context.SaveChanges();
    }
    
}

