// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.V1.Infrastructure;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Commands;
using FluentValidation;
using Swashbuckle.AspNetCore.Annotations;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;

namespace EdFi.Ods.AdminApi.V1.Features.OdsInstances;

public class AddOdsInstance : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPost(endpoints, "/odsInstances", Handle)
            .WithDefaultDescription()
            .WithRouteOptions(b => b.WithResponseCode(201))
            .BuildForVersions(AdminApiVersions.V1);
    }

    public async Task<IResult> Handle(Validator validator, IAddOdsInstanceCommand addOdsInstanceCommand, IMapper mapper, AddOdsInstanceRequest request)
    {
        await validator.GuardAsync(request);
        var addedOdsInstance = addOdsInstanceCommand.Execute(request);
        var model = mapper.Map<OdsInstanceModel>(addedOdsInstance);
        return AdminApiResponse<OdsInstanceModel>.Created(model, "odsInstance", $"/odsInstances/{model.OdsInstanceId}");
    }

    [SwaggerSchema(Title = "AddOdsInstanceRequest")]
    public class AddOdsInstanceRequest : IAddOdsInstanceModel
    {
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceName, Nullable = false)]
        public string? Name { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceInstanceType, Nullable = true)]
        public string? InstanceType { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceStatus, Nullable = true)]
        public string? Status { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceIsExtended, Nullable = true)]
        public bool? IsExtended { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceVersion, Nullable = true)]
        public string? Version { get; set; }
    }

    public class Validator : AbstractValidator<IAddOdsInstanceModel>
    {
        private readonly IGetOdsInstancesQuery _getOdsInstancesQuery;
        private readonly string _databaseEngine;
        public Validator(IGetOdsInstancesQuery getOdsInstancesQuery)
        {
            _getOdsInstancesQuery = getOdsInstancesQuery;

            RuleFor(m => m.Name)
                .NotEmpty()
                .Must(BeAUniqueName)
                .WithMessage(FeatureConstants.OdsInstanceAlreadyExistsMessage);

            RuleFor(m => m.Name)
                .MaximumLength(100)
                .When(m => !string.IsNullOrEmpty(m.Name));

            RuleFor(m => m.InstanceType)
                .NotEmpty();

            RuleFor(m => m.InstanceType)
                .MaximumLength(100)
                .When(m => !string.IsNullOrEmpty(m.InstanceType));

            RuleFor(m => m.Status)
                .NotEmpty();

            RuleFor(m => m.Status)
                .MaximumLength(100)
                .When(m => !string.IsNullOrEmpty(m.Status));

            RuleFor(m => m.Version)
                .NotEmpty();

            RuleFor(m => m.Version)
                .MaximumLength(20)
                .When(m => !string.IsNullOrEmpty(m.Version));
        }

        private bool BeAUniqueName(string? name)
        {
            return _getOdsInstancesQuery.Execute().TrueForAll(x => x.Name != name);
        }
    }
}
