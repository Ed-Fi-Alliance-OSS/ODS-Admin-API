// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;

public abstract class GetClaimSetByIdQueryBase(ISecurityContext securityContext)
{
    private readonly ISecurityContext _securityContext = securityContext;

    protected ClaimSet ExecuteCore(int securityContextClaimSetId)
    {
        var securityContextClaimSet = _securityContext.ClaimSets
            .SingleOrDefault(x => x.ClaimSetId == securityContextClaimSetId);

        if (securityContextClaimSet != null)
        {
            return new ClaimSet
            {
                Id = securityContextClaimSet.ClaimSetId,
                Name = securityContextClaimSet.ClaimSetName,
                IsEditable = !securityContextClaimSet.ForApplicationUseOnly && !securityContextClaimSet.IsEdfiPreset
            };
        }

        throw new NotFoundException<int>("claimset", securityContextClaimSetId);
    }
}
