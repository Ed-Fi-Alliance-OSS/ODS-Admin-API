// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Documentation;
using FluentValidation;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.Vendors;

public class EditVendor : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPut(endpoints, "/vendors/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> Handle(EditVendorCommand editVendorCommand, IMapper mapper,
                       Validator validator, EditVendorRequest request, int id)
    {
        request.Id = id;
        await validator.GuardAsync(request);
        editVendorCommand.Execute(request);
        return Results.Ok();
    }

    [SwaggerSchema(Title = "EditVendorRequest")]
    public class EditVendorRequest : IEditVendor
    {
        [SwaggerExclude]
        public int Id { get; set; }

        [SwaggerSchema(Description = FeatureConstants.VendorNameDescription, Nullable = false)]
        public string? Company { get; set; }

        [SwaggerSchema(Description = FeatureConstants.VendorNamespaceDescription, Nullable = false)]
        public string? NamespacePrefixes { get; set; }

        [SwaggerSchema(Description = FeatureConstants.VendorContactDescription, Nullable = false)]
        public string? ContactName { get; set; }

        [SwaggerSchema(Description = FeatureConstants.VendorContactEmailDescription, Nullable = false)]
        public string? ContactEmailAddress { get; set; }
    }

    public class Validator : AbstractValidator<EditVendorRequest>
    {
        public Validator()
        {
            RuleFor(m => m.Id).Must(id => id > 0).WithMessage("Please provide valid Vendor Id.");
            RuleFor(m => m.Company).NotEmpty();
            RuleFor(m => m.Company)
                .Must(name => !VendorExtensions.IsSystemReservedVendorName(name))
                .WithMessage(p => $"'{p.Company}' is a reserved name and may not be used. Please choose another name.");

            RuleFor(m => m.ContactName).NotEmpty();
            RuleFor(m => m.ContactEmailAddress).NotEmpty().EmailAddress();
        }
    }
}
