// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Documentation;
using FluentValidation;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.OdsInstanceContext;

public class EditOdsInstanceContext : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapPut(endpoints, "/odsInstanceContexts/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> Handle(Validator validator, IEditOdsInstanceContextCommand editOdsInstanceContextCommand, IMapper mapper, IUsersContext db, EditOdsInstanceContextRequest request, int id)
    {
        request.Id = id;
        await validator.GuardAsync(request);
        editOdsInstanceContextCommand.Execute(request);
        return Results.Ok();
    }


    [SwaggerSchema(Title = "EditOdsInstanceContextRequest")]
    public class EditOdsInstanceContextRequest : IEditOdsInstanceContextModel
    {
        [SwaggerExclude]
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceContextIdDescription, Nullable = false)]
        public int Id { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceContextOdsInstanceIdDescription, Nullable = false)]
        public int OdsInstanceId { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceContextContextKeyDescription, Nullable = false)]
        public string? ContextKey { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceContextContextValueDescription, Nullable = false)]
        public string? ContextValue { get; set; }
    }

    public class Validator : AbstractValidator<EditOdsInstanceContextRequest>
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

        private bool BeUniqueCombinedKey(EditOdsInstanceContextRequest request)
        {
            return !_getOdsInstanceContextsQuery.Execute().Exists
                (x => x.OdsInstance?.OdsInstanceId == request.OdsInstanceId &&
                x.ContextKey.Equals(request.ContextKey, StringComparison.OrdinalIgnoreCase) &&
                x.OdsInstanceContextId != request.Id);
        }
    }
}
