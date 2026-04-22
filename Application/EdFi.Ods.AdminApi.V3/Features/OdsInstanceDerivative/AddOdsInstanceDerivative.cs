// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FluentValidation;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
namespace EdFi.Ods.AdminApi.V3.Features.OdsInstanceDerivative;

public class AddOdsInstanceDerivative : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
           .MapPost(endpoints, "/odsInstanceDerivatives", Handle)
           .WithDefaultSummaryAndDescription()
           .WithRouteOptions(b => b.WithResponseCode(201))
           .BuildForVersions(AdminApiVersions.V3);
    }

    public static async Task<IResult> Handle(Validator validator, IAddOdsInstanceDerivativeCommand addOdsInstanceDerivativeCommand, AddOdsInstanceDerivativeRequest request, HttpContext httpContext)
    {
        await validator.GuardAsync(request);
        var addedOdsInstanceDerivative = addOdsInstanceDerivativeCommand.Execute(request);
        var absoluteLocation = ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V3, $"/odsInstanceDerivatives/{addedOdsInstanceDerivative.OdsInstanceDerivativeId}");
        return Results.Created(absoluteLocation, null);
    }

    [SwaggerSchema(Title = "AddOdsInstanceDerivativeRequest")]
    public class AddOdsInstanceDerivativeRequest : IAddOdsInstanceDerivativeModel
    {
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceDerivativeOdsInstanceIdDescription, Nullable = false)]
        public int OdsInstanceId { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceDerivativeDerivativeTypeDescription, Nullable = false)]
        public string? DerivativeType { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceDerivativeConnectionStringDescription, Nullable = false)]
        public string? ConnectionString { get; set; }
    }

    public class Validator : AbstractValidator<AddOdsInstanceDerivativeRequest>
    {
        private readonly IGetOdsInstanceQuery _getOdsInstanceQuery;
        private readonly IGetOdsInstanceDerivativesQuery _getOdsInstanceDerivativesQuery;
        private readonly string _databaseEngine;

        public Validator(IGetOdsInstanceQuery getOdsInstanceQuery, IGetOdsInstanceDerivativesQuery getOdsInstanceDerivativesQuery, IOptions<AppSettings> options)
        {
            _getOdsInstanceQuery = getOdsInstanceQuery;
            _getOdsInstanceDerivativesQuery = getOdsInstanceDerivativesQuery;
            _databaseEngine = options.Value.DatabaseEngine ?? throw new NotFoundException<string>("AppSettings", "DatabaseEngine");

            RuleFor(m => m.DerivativeType).NotEmpty();

            RuleFor(m => m.DerivativeType)
                .Matches("^(?i)(readreplica|snapshot)$")
                .WithMessage(FeatureConstants.OdsInstanceDerivativeDerivativeTypeNotValid)
                .When(m => !string.IsNullOrEmpty(m.DerivativeType));

            RuleFor(m => m.OdsInstanceId)
                .NotEqual(0)
                .WithMessage(FeatureConstants.OdsInstanceIdValidationMessage);

            RuleFor(m => m.OdsInstanceId)
                .Must(BeAnExistingOdsInstance)
                .When(m => !m.OdsInstanceId.Equals(0));

            RuleFor(m => m.ConnectionString)
                .NotEmpty();

            RuleFor(m => m.ConnectionString)
                .Must(BeAValidConnectionString)
                .WithMessage(FeatureConstants.OdsInstanceConnectionStringInvalid)
                .When(m => !string.IsNullOrEmpty(m.ConnectionString));

            RuleFor(odsDerivative => odsDerivative)
                .Must(BeUniqueCombinedKey)
                .WithMessage(FeatureConstants.OdsInstanceDerivativeCombinedKeyMustBeUnique);
        }

        private bool BeAnExistingOdsInstance(int id)
        {
            _getOdsInstanceQuery.Execute(id);
            return true;
        }

        private bool BeAValidConnectionString(string? connectionString)
        {
            return ConnectionStringHelper.ValidateConnectionString(_databaseEngine, connectionString);
        }

        private bool BeUniqueCombinedKey(AddOdsInstanceDerivativeRequest request)
        {
            return !_getOdsInstanceDerivativesQuery.Execute().Exists(x => x.OdsInstance?.OdsInstanceId == request.OdsInstanceId && x.DerivativeType.Equals(request.DerivativeType, StringComparison.OrdinalIgnoreCase));
        }

    }
}



