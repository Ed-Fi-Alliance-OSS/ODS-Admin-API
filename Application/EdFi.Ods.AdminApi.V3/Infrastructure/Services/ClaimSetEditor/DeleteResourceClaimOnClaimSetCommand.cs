// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Security.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;

public interface IDeleteResouceClaimOnClaimSetCommand
{
    void Execute(int claimSetId, int resourceClaimId);
}

public class DeleteResouceClaimOnClaimSetCommand(ISecurityContext context)
    : DeleteResourceClaimOnClaimSetCommandBase(context), IDeleteResouceClaimOnClaimSetCommand
{
    public void Execute(int claimSetId, int resourceClaimId)
    {
        ExecuteCore(claimSetId, resourceClaimId);
    }
    
}

