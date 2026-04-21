// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Security.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;
namespace EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;

public class AddClaimSetCommand(ISecurityContext context)
    : AddClaimSetCommandBase(context)
{
    public int Execute(IAddClaimSetModel claimSet)
    {
        return ExecuteCore(claimSet);
    }
}

public interface IAddClaimSetModel : IAddClaimSetModelCommon
{
}

public class AddClaimSetModel : IAddClaimSetModel
{
    public string? ClaimSetName { get; set; }
}

