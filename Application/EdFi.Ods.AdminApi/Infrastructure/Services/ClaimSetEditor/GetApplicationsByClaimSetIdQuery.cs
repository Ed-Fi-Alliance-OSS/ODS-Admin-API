// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor;
using EdFi.Security.DataAccess.Contexts;
using CommonApplication = EdFi.Ods.AdminApi.Common.Infrastructure.ClaimSetEditor.ApplicationModel;

namespace EdFi.Ods.AdminApi.Infrastructure.ClaimSetEditor;

public class GetApplicationsByClaimSetIdQuery(ISecurityContext securityContext, IUsersContext usersContext)
    : GetApplicationsByClaimSetIdQueryBase(securityContext, usersContext), IGetApplicationsByClaimSetIdQuery
{
    public IEnumerable<Application> Execute(int securityContextClaimSetId)
    {
        return ExecuteCore(securityContextClaimSetId)
            .Select(Map)
            .ToList();
    }

    public int ExecuteCount(int claimSetId)
    {
        return ExecuteCountCore(claimSetId);
    }

    private static Application Map(CommonApplication application)
    {
        return new Application
        {
            Name = application.Name,
            VendorName = application.VendorName
        };
    }
}

public interface IGetApplicationsByClaimSetIdQuery
{
    IEnumerable<Application> Execute(int securityContextClaimSetId);

    int ExecuteCount(int claimSetId);
}
