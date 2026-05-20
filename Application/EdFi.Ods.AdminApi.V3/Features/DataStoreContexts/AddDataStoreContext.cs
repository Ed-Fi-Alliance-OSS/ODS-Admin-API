// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FluentValidation;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;

public class AddDataStoreContext : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
           .MapPost(endpoints, "/dataStoreContexts", Handle)
           .WithDefaultSummaryAndDescription()
           .WithRouteOptions(b => b.WithResponseCode(201))
           .BuildForVersions(AdminApiVersions.V3);
    }

    public static async Task<IResult> Handle(Validator validator, IAddDataStoreContextCommand addDataStoreContextCommand, AddDataStoreContextRequest request, HttpContext httpContext)
    {
        await validator.GuardAsync(request);
        var added = addDataStoreContextCommand.Execute(request);
        var absoluteLocation = ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V3, $"/dataStoreContexts/{added.OdsInstanceContextId}");
        return Results.Created(absoluteLocation, null);
    }

    [SwaggerSchema(Title = "AddDataStoreContextRequest")]
    public class AddDataStoreContextRequest : IAddDataStoreContextModel
    {
        [SwaggerSchema(Description = FeatureConstants.DataStoreContextDataStoreIdDescription, Nullable = false)]
        public int DataStoreId { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceContextContextKeyDescription, Nullable = false)]
        public string? ContextKey { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceContextContextValueDescription, Nullable = false)]
        public string? ContextValue { get; set; }
    }

    public class Validator : AbstractValidator<AddDataStoreContextRequest>
    {
        private readonly IGetDataStoreQuery _getDataStoreQuery;
        private readonly IGetDataStoreContextsQuery _getDataStoreContextsQuery;

        public Validator(IGetDataStoreQuery getDataStoreQuery, IGetDataStoreContextsQuery getDataStoreContextsQuery)
        {
            _getDataStoreQuery = getDataStoreQuery;
            _getDataStoreContextsQuery = getDataStoreContextsQuery;

            RuleFor(m => m.ContextKey).NotEmpty();
            RuleFor(m => m.ContextValue).NotEmpty();

            RuleFor(m => m.DataStoreId)
                .NotEqual(0)
                .WithMessage(FeatureConstants.DataStoreIdValidationMessage);

            RuleFor(m => m.DataStoreId)
                .Must(BeAnExistingDataStore)
                .When(m => !m.DataStoreId.Equals(0));

            RuleFor(ctx => ctx)
                .Must(BeUniqueCombinedKey)
                .WithMessage(FeatureConstants.DataStoreContextCombinedKeyMustBeUnique);
        }

        private bool BeAnExistingDataStore(int id)
        {
            _getDataStoreQuery.Execute(id);
            return true;
        }

        private bool BeUniqueCombinedKey(AddDataStoreContextRequest request)
        {
            return !_getDataStoreContextsQuery.Execute().Exists(
                x => x.OdsInstance?.OdsInstanceId == request.DataStoreId &&
                x.ContextKey.Equals(request.ContextKey, StringComparison.OrdinalIgnoreCase));
        }
    }
}
