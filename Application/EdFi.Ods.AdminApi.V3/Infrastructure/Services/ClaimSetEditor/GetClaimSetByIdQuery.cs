// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;
using EdFi.Security.DataAccess.Contexts;
using CommonClaimSet = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.ClaimSet;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;

public class GetClaimSetByIdQuery(ISecurityContext securityContext)
    : GetClaimSetByIdQueryBase(securityContext), IGetClaimSetByIdQuery
{
    public ClaimSet Execute(int securityContextClaimSetId)
    {
        return Map(ExecuteCore(securityContextClaimSetId));
    }

    private static ClaimSet Map(CommonClaimSet claimSet)
    {
        return new ClaimSet
        {
            Id = claimSet.Id,
            Name = claimSet.Name,
            IsEditable = claimSet.IsEditable,
            ApplicationsCount = claimSet.ApplicationsCount
        };
    }
}

public interface IGetClaimSetByIdQuery
{
    ClaimSet Execute(int securityContextClaimSetId);
}




