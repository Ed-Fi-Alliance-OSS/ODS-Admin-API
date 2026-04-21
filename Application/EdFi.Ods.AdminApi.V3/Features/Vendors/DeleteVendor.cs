// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Extensions;

namespace EdFi.Ods.AdminApi.V3.Features.Vendors;

public class DeleteVendor : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapDelete(endpoints, "/vendors/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(200, FeatureCommonConstants.DeletedSuccessResponseDescription))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public static Task<IResult> Handle(DeleteVendorCommand deleteVendorCommand, int id)
    {
        deleteVendorCommand.Execute(id);
        return Task.FromResult(Results.Ok("Vendor".ToJsonObjectResponseDeleted()));
    }
}



