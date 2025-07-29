// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;

namespace EdFi.Ods.AdminApi.Features.Vendors;

public class ReadVendor : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapGet(endpoints, "/vendors", GetVendors)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<VendorModel[]>(200))
            .BuildForVersions(AdminApiVersions.V2);

        AdminApiEndpointBuilder.MapGet(endpoints, "/vendors/{id}", GetVendor)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponse<VendorModel>(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    internal static Task<IResult> GetVendors(
        IGetVendorsQuery getVendorsQuery, IMapper mapper, [AsParameters] CommonQueryParams commonQueryParams, int? id, string? company, string? namespacePrefixes, string? contactName, string? contactEmailAddress)
    {
        var vendorList = mapper.Map<List<VendorModel>>(getVendorsQuery.Execute(
            commonQueryParams,
            id, company, namespacePrefixes, contactName, contactEmailAddress));
        return Task.FromResult(Results.Ok(vendorList));
    }

    internal static Task<IResult> GetVendor(IGetVendorByIdQuery getVendorByIdQuery, IMapper mapper, int id)
    {
        var vendor = getVendorByIdQuery.Execute(id);
        if (vendor == null)
        {
            throw new NotFoundException<int>("vendor", id);
        }
        var model = mapper.Map<VendorModel>(vendor);
        return Task.FromResult(Results.Ok(model));
    }
}
