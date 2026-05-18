// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.V3.Infrastructure.Documentation;
using FluentValidation;
using Microsoft.Extensions.Options;
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
        [SwaggerSchema(Description = FeatureConstants.DataStoreDerivativeConnectionStringDescription, Nullable = false)]
        public string? ConnectionString { get; set; }
    }

    public class Validator : AbstractValidator<EditDataStoreDerivativeRequest>
    {
        private readonly IGetDataStoreQuery _getDataStoreQuery;
        private readonly IGetDataStoreDerivativesQuery _getDataStoreDerivativesQuery;
        private readonly string _databaseEngine;

        public Validator(IGetDataStoreQuery getDataStoreQuery, IGetDataStoreDerivativesQuery getDataStoreDerivativesQuery, IOptions<AppSettings> options)
        {
            _getDataStoreQuery = getDataStoreQuery;
            _getDataStoreDerivativesQuery = getDataStoreDerivativesQuery;
            _databaseEngine = options.Value.DatabaseEngine ?? DatabaseEngineEnum.SqlServer;

            RuleFor(m => m.DerivativeType).NotEmpty();

            RuleFor(m => m.DerivativeType)
                .Matches("^(?i)(readreplica|snapshot)$")
                .WithMessage(FeatureConstants.DataStoreDerivativeTypeNotValid)
                .When(m => !string.IsNullOrEmpty(m.DerivativeType));

            RuleFor(m => m.DataStoreId)
                .NotEqual(0)
                .WithMessage(FeatureConstants.DataStoreIdValidationMessage);

            RuleFor(m => m.DataStoreId)
                .Must(BeAnExistingDataStore)
                .When(m => !m.DataStoreId.Equals(0));

            RuleFor(m => m.ConnectionString)
                .Must(BeAValidConnectionString)
                .WithMessage(FeatureConstants.DataStoreConnectionStringInvalid)
                .When(m => !string.IsNullOrWhiteSpace(m.ConnectionString));

            RuleFor(ctx => ctx)
                .Must(BeUniqueCombinedKey)
                .WithMessage(FeatureConstants.DataStoreDerivativeCombinedKeyMustBeUnique);
        }

        private bool BeAnExistingDataStore(int id)
        {
            _getDataStoreQuery.Execute(id);
            return true;
        }

        private bool BeAValidConnectionString(string? connectionString)
        {
            return ConnectionStringHelper.ValidateConnectionString(_databaseEngine, connectionString);
        }

        private bool BeUniqueCombinedKey(EditDataStoreDerivativeRequest request)
        {
            return !_getDataStoreDerivativesQuery.Execute().Exists(
                x => x.OdsInstance?.OdsInstanceId == request.DataStoreId &&
                (x.DerivativeType?.Equals(request.DerivativeType, StringComparison.OrdinalIgnoreCase) == true) &&
                x.OdsInstanceDerivativeId != request.Id);
        }
    }
}
