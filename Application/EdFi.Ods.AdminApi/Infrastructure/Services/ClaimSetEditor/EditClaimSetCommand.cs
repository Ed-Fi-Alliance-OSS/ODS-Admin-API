// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;
using EdFi.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor
{
    public interface IEditClaimSetCommand
    {
        int Execute(IEditClaimSetModel claimSet);
    }

    public class EditClaimSetCommand(ISecurityContext securityContext, IUsersContext usersContext)
        : EditClaimSetCommandBase(securityContext, usersContext), IEditClaimSetCommand
    {
        public int Execute(IEditClaimSetModel claimSet)
        {
            return ExecuteCore(claimSet);
        }
    }

    public interface IEditClaimSetModel : IEditClaimSetModelCommon
    {
    }
}
