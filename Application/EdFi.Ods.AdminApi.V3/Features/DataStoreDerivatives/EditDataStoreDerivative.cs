// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.V3.Infrastructure.Documentation;
using FluentValidation;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;

public class EditDataStoreDerivative : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapPut(endpoints, "/dataStoreDerivatives/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(204))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public static async Task<IResult> Handle(Validator validator, IEditDataStoreDerivativeCommand editDataStoreDerivativeCommand, EditDataStoreDerivativeRequest request, int id)
    {
        request.Id = id;
        await validator.GuardAsync(request);
        editDataStoreDerivativeCommand.Execute(request);
        return Results.NoContent();
    }

    [SwaggerSchema(Title = "EditDataStoreDerivativeRequest")]
    public class EditDataStoreDerivativeRequest : IEditDataStoreDerivativeModel
    {
        [SwaggerExclude]
        [SwaggerSchema(Description = FeatureConstants.DataStoreDerivativeIdDescription, Nullable = false)]
        public int Id { get; set; }
        [SwaggerSchema(Description = FeatureConstants.DataStoreDerivativeDataStoreIdDescription, Nullable = false)]
        public int DataStoreId { get; set; }
        [SwaggerSchema(Description = FeatureConstants.DataStoreDerivativeTypeDescription, Nullable = false)]
        public string? DerivativeType { get; set; }
    }

    public class Validator : AbstractValidator<EditDataStoreDerivativeRequest>
    {
        private readonly IGetDataStoreQuery _getDataStoreQuery;
        private readonly IGetDataStoreDerivativesQuery _getDataStoreDerivativesQuery;

        public Validator(IGetDataStoreQuery getDataStoreQuery, IGetDataStoreDerivativesQuery getDataStoreDerivativesQuery)
        {
            _getDataStoreQuery = getDataStoreQuery;
            _getDataStoreDerivativesQuery = getDataStoreDerivativesQuery;

            RuleFor(m => m.DerivativeType).NotEmpty();

            RuleFor(m => m.DataStoreId)
                .NotEqual(0)
                .WithMessage(FeatureConstants.DataStoreIdValidationMessage);

            RuleFor(m => m.DataStoreId)
                .Must(BeAnExistingDataStore)
                .When(m => !m.DataStoreId.Equals(0));

            RuleFor(ctx => ctx)
                .Must(BeUniqueCombinedKey)
                .WithMessage(FeatureConstants.DataStoreDerivativeCombinedKeyMustBeUnique);
        }

        private bool BeAnExistingDataStore(int id)
        {
            _getDataStoreQuery.Execute(id);
            return true;
        }

        private bool BeUniqueCombinedKey(EditDataStoreDerivativeRequest request)
        {
            return !_getDataStoreDerivativesQuery.Execute().Exists(
                x => x.OdsInstance?.OdsInstanceId == request.DataStoreId &&
                x.DerivativeType.Equals(request.DerivativeType, StringComparison.OrdinalIgnoreCase) &&
                x.OdsInstanceDerivativeId != request.Id);
        }
    }
}
