// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using EdFi.Ods.AdminApi.V1.Security.DataAccess.Contexts;

namespace EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;

public class AddClaimSetCommandService
{
    private readonly ISecurityContext _context;

    public AddClaimSetCommandService(ISecurityContext context)
    {
        _context = context;
    }

    public int Execute(IAddClaimSetModel claimSet)
    {
        var newClaimSet = new Security.DataAccess.Models.ClaimSet
        {
            ClaimSetName = claimSet.ClaimSetName,
            Application = _context.Applications.Single(),
            IsEdfiPreset = false,
            ForApplicationUseOnly = false
        };
        _context.ClaimSets.Add(newClaimSet);
        _context.SaveChanges();

        return newClaimSet.ClaimSetId;
    }
}
