// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.V2.Infrastructure.ClaimSetEditor;

public class GetClaimSetByIdQuery : IGetClaimSetByIdQuery
{
    private readonly ISecurityContext _securityContext;

    public GetClaimSetByIdQuery(ISecurityContext securityContext)
    {
        _securityContext = securityContext;
    }

    public ClaimSet Execute(int securityContextClaimSetId)
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

public interface IGetClaimSetByIdQuery
{
    ClaimSet Execute(int securityContextClaimSetId);
}

