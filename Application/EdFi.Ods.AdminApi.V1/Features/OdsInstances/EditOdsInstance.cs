// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Queries;
using FluentValidation;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V1.Features.OdsInstances;

public class EditOdsInstance : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapPut(endpoints, "/odsInstances/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(200))
            .BuildForVersions(AdminApiVersions.V1);
    }

    public async Task<IResult> Handle(
        Validator validator,
        IEditOdsInstanceCommand editOdsInstanceCommand,
        IMapper mapper,
        EditOdsInstanceRequest request,
        int id
    )
    {
        request.OdsInstanceId = id;
        await validator.GuardAsync(request);
        var updatedOdsInstance = editOdsInstanceCommand.Execute(request);
        var model = mapper.Map<OdsInstanceModel>(updatedOdsInstance);
        return AdminApiResponse<OdsInstanceModel>.Updated(model, "odsInstance");
    }

    [SwaggerSchema(Title = "EditOdsInstanceRequest")]
    public class EditOdsInstanceRequest : IEditOdsInstanceModel
    {
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceIdDescription, Nullable = false)]
        public int OdsInstanceId { get; set; }

        [SwaggerSchema(Description = FeatureConstants.OdsInstanceName, Nullable = false)]
        public string Name { get; set; } = string.Empty;

        [SwaggerSchema(Description = FeatureConstants.OdsInstanceInstanceType, Nullable = true)]
        public string InstanceType { get; set; } = string.Empty;

        [SwaggerSchema(Description = FeatureConstants.OdsInstanceStatus, Nullable = true)]
        public string Status { get; set; } = string.Empty;

        [SwaggerSchema(Description = FeatureConstants.OdsInstanceIsExtended, Nullable = true)]
        public bool? IsExtended { get; set; }

        [SwaggerSchema(Description = FeatureConstants.OdsInstanceVersion, Nullable = true)]
        public string Version { get; set; } = string.Empty;
    }

    public class Validator : AbstractValidator<IEditOdsInstanceModel>
    {
        private readonly IGetOdsInstancesQuery _getOdsInstancesQuery;
        private readonly IGetOdsInstanceQuery _getOdsInstanceQuery;

        public Validator(IGetOdsInstancesQuery getOdsInstancesQuery, IGetOdsInstanceQuery getOdsInstanceQuery)
        {
            _getOdsInstancesQuery = getOdsInstancesQuery;
            _getOdsInstanceQuery = getOdsInstanceQuery;

            RuleFor(m => m.OdsInstanceId)
                .Must(id => id > 0)
                .WithMessage("Please provide valid Ods instance Id.");

            RuleFor(m => m.Name)
                .NotEmpty()
                .Must(BeAUniqueName)
                .WithMessage(FeatureConstants.OdsInstanceAlreadyExistsMessage)
                .When(m => BeAnExistingOdsInstance(m.OdsInstanceId) && NameIsChanged(m));

            RuleFor(m => m.Name).NotEmpty().MaximumLength(100).When(m => !string.IsNullOrEmpty(m.Name));

            RuleFor(m => m.InstanceType)
                .NotEmpty()
                .MaximumLength(100)
                .When(m => !string.IsNullOrEmpty(m.InstanceType));

            RuleFor(m => m.Status).NotEmpty().MaximumLength(100).When(m => !string.IsNullOrEmpty(m.Status));

            RuleFor(m => m.Version).NotEmpty().MaximumLength(20).When(m => !string.IsNullOrEmpty(m.Version));
        }

        private bool BeAnExistingOdsInstance(int id)
        {
            _getOdsInstanceQuery.Execute(id);
            return true;
        }

        private bool NameIsChanged(IEditOdsInstanceModel model)
        {
            return _getOdsInstanceQuery.Execute(model.OdsInstanceId).Name != model.Name;
        }

        private bool BeAUniqueName(string? name)
        {
            return _getOdsInstancesQuery.Execute().TrueForAll(x => x.Name != name);
        }
    }
}
