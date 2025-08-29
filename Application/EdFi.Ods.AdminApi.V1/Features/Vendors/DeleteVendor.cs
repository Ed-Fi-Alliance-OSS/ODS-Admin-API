// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Commands;

namespace EdFi.Ods.AdminApi.V1.Features.Vendors;

public class DeleteVendor : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapDelete(endpoints, "/vendors/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(200, FeatureConstants.DeletedSuccessResponseDescription))
            .BuildForVersions(AdminApiVersions.V1);
    }

    public Task<IResult> Handle(DeleteVendorCommand deleteVendorCommand, int id)
    {
        deleteVendorCommand.Execute(id);
        return Task.FromResult(AdminApiResponse.Deleted("Vendor"));
    }
}
