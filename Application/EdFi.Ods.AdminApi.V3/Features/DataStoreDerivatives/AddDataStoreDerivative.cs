// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FluentValidation;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;

public class AddDataStoreDerivative : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
           .MapPost(endpoints, "/dataStoreDerivatives", Handle)
           .WithDefaultSummaryAndDescription()
           .WithRouteOptions(b => b.WithResponseCode(201))
           .BuildForVersions(AdminApiVersions.V3);
    }

    public static async Task<IResult> Handle(Validator validator, IAddDataStoreDerivativeCommand addDataStoreDerivativeCommand, AddDataStoreDerivativeRequest request, HttpContext httpContext)
    {
        await validator.GuardAsync(request);
        var added = addDataStoreDerivativeCommand.Execute(request);
        var absoluteLocation = ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V3, $"/dataStoreDerivatives/{added.OdsInstanceDerivativeId}");
        return Results.Created(absoluteLocation, null);
    }

    [SwaggerSchema(Title = "AddDataStoreDerivativeRequest")]
    public class AddDataStoreDerivativeRequest : IAddDataStoreDerivativeModel
    {
        [SwaggerSchema(Description = FeatureConstants.DataStoreDerivativeDataStoreIdDescription, Nullable = false)]
        public int DataStoreId { get; set; }
        [SwaggerSchema(Description = FeatureConstants.DataStoreDerivativeTypeDescription, Nullable = false)]
        public string? DerivativeType { get; set; }
        [SwaggerSchema(Description = FeatureConstants.DataStoreDerivativeConnectionStringDescription, Nullable = false)]
        public string? ConnectionString { get; set; }
    }

    public class Validator : AbstractValidator<AddDataStoreDerivativeRequest>
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
                .NotEmpty();

            RuleFor(m => m.ConnectionString)
                .Must(BeAValidConnectionString)
                .WithMessage(FeatureConstants.DataStoreConnectionStringInvalid)
                .When(m => !string.IsNullOrEmpty(m.ConnectionString));

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

        private bool BeUniqueCombinedKey(AddDataStoreDerivativeRequest request)
        {
            return !_getDataStoreDerivativesQuery.Execute().Exists(
                x => x.OdsInstance?.OdsInstanceId == request.DataStoreId &&
                (x.DerivativeType?.Equals(request.DerivativeType, StringComparison.OrdinalIgnoreCase) == true));
        }
    }
}
