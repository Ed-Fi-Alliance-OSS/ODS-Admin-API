// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Security.DataAccess.Contexts;
using EdFi.Security.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Services.ClaimSetEditor
{
    public interface ICopyClaimSetCommand
    {
        int Execute(ICopyClaimSetModel claimSet);
    }

    public class CopyClaimSetCommand(ISecurityContext context)
        : CopyClaimSetCommandBase(context), ICopyClaimSetCommand
    {
        public int Execute(ICopyClaimSetModel claimSet)
        {
            return ExecuteCore(claimSet);
        }
    }

    public interface ICopyClaimSetModel : ICopyClaimSetModelCommon
    {
    }
}

