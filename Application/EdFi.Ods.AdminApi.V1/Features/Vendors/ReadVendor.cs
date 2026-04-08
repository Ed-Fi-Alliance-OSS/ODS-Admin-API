// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V1.Features.Vendors;

public class ReadVendor : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/vendors", GetVendors)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<VendorModel[]>(200))
            .BuildForVersions(AdminApiVersions.V1);

        AdminApiEndpointBuilder.MapGet(endpoints, "/vendors/{id}", GetVendor)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<VendorModel>(200))
            .BuildForVersions(AdminApiVersions.V1);
    }

    internal Task<IResult> GetVendors(IGetVendorsQuery getVendorsQuery, [AsParameters] CommonQueryParams commonQueryParams)
    {
        var vendorList = VendorMapper.ToModelList(getVendorsQuery.Execute(commonQueryParams));
        return Task.FromResult(AdminApiResponse<List<VendorModel>>.Ok(vendorList));
    }

    internal Task<IResult> GetVendor(IGetVendorByIdQuery getVendorByIdQuery, int id)
    {
        var vendor = getVendorByIdQuery.Execute(id);
        if (vendor == null)
        {
            throw new NotFoundException<int>("vendor", id);
        }
        var model = VendorMapper.ToModel(vendor);
        return Task.FromResult(AdminApiResponse<VendorModel>.Ok(model));
    }
}
