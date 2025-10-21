// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Security.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Security.DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;

public class EditResourceOnClaimSetCommandService
{
    private readonly ISecurityContext _context;

    public EditResourceOnClaimSetCommandService(ISecurityContext context)
    {
        _context = context;
    }

    public void Execute(IEditResourceOnClaimSetModel model)
    {
        var resourceClaimToEdit = model.ResourceClaim;
        if (resourceClaimToEdit is null) return;

        var claimSetToEdit = _context.ClaimSets.Single(x => x.ClaimSetId == model.ClaimSetId);

        var claimSetResourceClaimsToEdit = _context.ClaimSetResourceClaimActions
            .Include(x => x.ResourceClaim)
            .Include(x => x.Action)
            .Include(x => x.ClaimSet)
            .Where(x => x.ResourceClaim.ResourceClaimId == resourceClaimToEdit.Id && x.ClaimSet.ClaimSetId == claimSetToEdit.ClaimSetId)
            .ToList();

        AddEnabledActionsToClaimSet(resourceClaimToEdit, claimSetResourceClaimsToEdit, claimSetToEdit);

        RemoveDisabledActionsFromClaimSet(resourceClaimToEdit, claimSetResourceClaimsToEdit);

        _context.SaveChanges();
    }

    private void RemoveDisabledActionsFromClaimSet(ResourceClaim modelResourceClaim, IEnumerable<ClaimSetResourceClaimAction> resourceClaimsToEdit)
    {
        var recordsToRemove = new List<ClaimSetResourceClaimAction>();

        foreach (var claimSetResourceClaim in resourceClaimsToEdit)
        {
            if (claimSetResourceClaim.Action.ActionName == Action.Create.Value && !modelResourceClaim.Create)
            {
                recordsToRemove.Add(claimSetResourceClaim);
            }
            else if (claimSetResourceClaim.Action.ActionName == Action.Read.Value && !modelResourceClaim.Read)
            {
                recordsToRemove.Add(claimSetResourceClaim);
            }
            else if (claimSetResourceClaim.Action.ActionName == Action.Update.Value && !modelResourceClaim.Update)
            {
                recordsToRemove.Add(claimSetResourceClaim);
            }
            else if (claimSetResourceClaim.Action.ActionName == Action.Delete.Value && !modelResourceClaim.Delete)
            {
                recordsToRemove.Add(claimSetResourceClaim);
            }
            else if (claimSetResourceClaim.Action.ActionName == Action.ReadChanges.Value && !modelResourceClaim.ReadChanges)
            {
                recordsToRemove.Add(claimSetResourceClaim);
            }
        }

        if (recordsToRemove.Any())
        {
            _context.ClaimSetResourceClaimActions.RemoveRange(recordsToRemove);
        }
    }

    private void AddEnabledActionsToClaimSet(ResourceClaim modelResourceClaim, IReadOnlyCollection<ClaimSetResourceClaimAction> claimSetResourceClaimsToEdit, Security.DataAccess.Models.ClaimSet claimSetToEdit)
    {
        var actionsFromDb = _context.Actions.ToList();

        var resourceClaimFromDb = _context.ResourceClaims.Single(x => x.ResourceClaimId == modelResourceClaim.Id);

        var recordsToAdd = new List<ClaimSetResourceClaimAction>();

        if (modelResourceClaim.Create && claimSetResourceClaimsToEdit.All(x => x.Action.ActionName != Action.Create.Value))
        {
            recordsToAdd.Add(new ClaimSetResourceClaimAction
            {
                Action = actionsFromDb.Single(x => x.ActionName == Action.Create.Value),
                ClaimSet = claimSetToEdit,
                ResourceClaim = resourceClaimFromDb
            });
        }

        if (modelResourceClaim.Read && claimSetResourceClaimsToEdit.All(x => x.Action.ActionName != Action.Read.Value))
        {
            recordsToAdd.Add(new ClaimSetResourceClaimAction
            {
                Action = actionsFromDb.Single(x => x.ActionName == Action.Read.Value),
                ClaimSet = claimSetToEdit,
                ResourceClaim = resourceClaimFromDb
            });
        }

        if (modelResourceClaim.Update && claimSetResourceClaimsToEdit.All(x => x.Action.ActionName != Action.Update.Value))
        {
            recordsToAdd.Add(new ClaimSetResourceClaimAction
            {
                Action = actionsFromDb.Single(x => x.ActionName == Action.Update.Value),
                ClaimSet = claimSetToEdit,
                ResourceClaim = resourceClaimFromDb
            });
        }

        if (modelResourceClaim.Delete && claimSetResourceClaimsToEdit.All(x => x.Action.ActionName != Action.Delete.Value))
        {
            recordsToAdd.Add(new ClaimSetResourceClaimAction
            {
                Action = actionsFromDb.Single(x => x.ActionName == Action.Delete.Value),
                ClaimSet = claimSetToEdit,
                ResourceClaim = resourceClaimFromDb
            });
        }

        if (modelResourceClaim.ReadChanges && claimSetResourceClaimsToEdit.All(x => x.Action.ActionName != Action.ReadChanges.Value))
        {
            recordsToAdd.Add(new ClaimSetResourceClaimAction
            {
                Action = actionsFromDb.Single(x => x.ActionName == Action.ReadChanges.Value),
                ClaimSet = claimSetToEdit,
                ResourceClaim = resourceClaimFromDb
            });
        }

        if (recordsToAdd.Any())
        {
            _context.ClaimSetResourceClaimActions.AddRange(recordsToAdd);
        }
    }
}
