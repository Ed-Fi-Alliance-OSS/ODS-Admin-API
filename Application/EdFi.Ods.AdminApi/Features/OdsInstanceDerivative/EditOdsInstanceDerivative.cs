// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Documentation;
using FluentValidation;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;
namespace EdFi.Ods.AdminApi.Features.OdsInstanceDerivative;

public class EditOdsInstanceDerivative : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapPut(endpoints, "/odsInstanceDerivatives/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(200))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> Handle(Validator validator, IEditOdsInstanceDerivativeCommand editOdsInstanceDerivativeCommand, IMapper mapper, IUsersContext db, EditOdsInstanceDerivativeRequest request, int id)
    {
        request.Id = id;
        SetCurrentConnectionString(db, request, id);
        await validator.GuardAsync(request);
        editOdsInstanceDerivativeCommand.Execute(request);
        return Results.Ok();
    }

    private static void SetCurrentConnectionString(IUsersContext db, EditOdsInstanceDerivativeRequest request, int id)
    {
        if (string.IsNullOrEmpty(request.ConnectionString))
            request.ConnectionString = db.OdsInstanceDerivatives.Find(id)?.ConnectionString;
    }

    [SwaggerSchema(Title = "EditOdsInstanceDerivativeRequest")]
    public class EditOdsInstanceDerivativeRequest : IEditOdsInstanceDerivativeModel
    {
        [SwaggerExclude]
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceDerivativeIdDescription, Nullable = false)]
        public int Id { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceDerivativeOdsInstanceIdDescription, Nullable = false)]
        public int OdsInstanceId { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceDerivativeDerivativeTypeDescription, Nullable = false)]
        public string? DerivativeType { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceDerivativeConnectionStringDescription, Nullable = false)]
        public string? ConnectionString { get; set; }
    }

    public class Validator : AbstractValidator<EditOdsInstanceDerivativeRequest>
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
                .Must(BeAValidConnectionString)
                .WithMessage(FeatureConstants.OdsInstanceConnectionStringInvalid)
                .When(m => !string.IsNullOrWhiteSpace(m.ConnectionString));

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

        private bool BeUniqueCombinedKey(EditOdsInstanceDerivativeRequest request)
        {
            return !_getOdsInstanceDerivativesQuery.Execute().Exists
                (x =>
                    x.OdsInstance?.OdsInstanceId == request.OdsInstanceId &&
                    x.DerivativeType.Equals(request.DerivativeType, StringComparison.OrdinalIgnoreCase) &&
                    x.OdsInstanceDerivativeId != request.Id);
        }
    }
}
