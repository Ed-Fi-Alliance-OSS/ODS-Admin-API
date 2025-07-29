// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using FluentValidation;
using Swashbuckle.AspNetCore.Annotations;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
namespace EdFi.Ods.AdminApi.Features.OdsInstanceContext;

public class AddOdsInstanceContext : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
           .MapPost(endpoints, "/odsInstanceContexts", Handle)
           .WithDefaultSummaryAndDescription()
           .WithRouteOptions(b => b.WithResponseCode(201))
           .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> Handle(Validator validator, IAddOdsInstanceContextCommand addOdsInstanceContextCommand, IMapper mapper, AddOdsInstanceContextRequest request)
    {
        await validator.GuardAsync(request);
        var addedOdsInstanceContext = addOdsInstanceContextCommand.Execute(request);
        return Results.Created($"/odsInstanceContexts/{addedOdsInstanceContext.OdsInstanceContextId}", null);
    }


    [SwaggerSchema(Title = "AddOdsInstanceContextRequest")]
    public class AddOdsInstanceContextRequest : IAddOdsInstanceContextModel
    {
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceContextOdsInstanceIdDescription, Nullable = false)]
        public int OdsInstanceId { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceContextContextKeyDescription, Nullable = false)]
        public string? ContextKey { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceContextContextValueDescription, Nullable = false)]
        public string? ContextValue { get; set; }
    }

    public class Validator : AbstractValidator<AddOdsInstanceContextRequest>
    {
        private readonly IGetOdsInstanceQuery _getOdsInstanceQuery;
        private readonly IGetOdsInstanceContextsQuery _getOdsInstanceContextsQuery;

        public Validator(IGetOdsInstanceQuery getOdsInstanceQuery, IGetOdsInstanceContextsQuery getOdsInstanceContextsQuery)
        {
            _getOdsInstanceQuery = getOdsInstanceQuery;
            _getOdsInstanceContextsQuery = getOdsInstanceContextsQuery;
            RuleFor(m => m.ContextKey).NotEmpty();

            RuleFor(m => m.ContextValue).NotEmpty();

            RuleFor(m => m.OdsInstanceId)
                .NotEqual(0)
                .WithMessage(FeatureConstants.OdsInstanceIdValidationMessage);

            RuleFor(m => m.OdsInstanceId)
                .Must(BeAnExistingOdsInstance)
                .When(m => !m.OdsInstanceId.Equals(0));

            RuleFor(odsContext => odsContext)
                 .Must(BeUniqueCombinedKey)
                 .WithMessage(FeatureConstants.OdsInstanceContextCombinedKeyMustBeUnique);

        }

        private bool BeAnExistingOdsInstance(int id)
        {
            _getOdsInstanceQuery.Execute(id);
            return true;
        }

        private bool BeUniqueCombinedKey(AddOdsInstanceContextRequest request)
        {
            return !_getOdsInstanceContextsQuery.Execute().Exists
                (x => x.OdsInstance?.OdsInstanceId == request.OdsInstanceId &&
                x.ContextKey.Equals(request.ContextKey, StringComparison.OrdinalIgnoreCase));
        }
    }
}
