// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FluentValidation;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.OdsInstances;

public class AddOdsInstance : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
           .MapPost(endpoints, "/odsInstances", Handle)
           .WithDefaultSummaryAndDescription()
           .WithRouteOptions(b => b.WithResponseCode(201))
           .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> Handle(
        Validator validator,
        IAddOdsInstanceCommand addOdsInstanceCommand,
        IMapper mapper,
        ISymmetricStringEncryptionProvider encryptionProvider,
        IOptions<AppSettings> options,
        AddOdsInstanceRequest request)
    {
        await validator.GuardAsync(request);
        string encryptionKey = options.Value.EncryptionKey ?? throw new InvalidOperationException("EncryptionKey can't be null.");
        request.ConnectionString = encryptionProvider.Encrypt(request.ConnectionString, Convert.FromBase64String(encryptionKey));
        var addedProfile = addOdsInstanceCommand.Execute(request);
        return Results.Created($"/odsInstances/{addedProfile.OdsInstanceId}", null);
    }

    [SwaggerSchema(Title = "AddOdsInstanceRequest")]
    public class AddOdsInstanceRequest : IAddOdsInstanceModel
    {
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceName, Nullable = false)]
        public string? Name { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceInstanceType, Nullable = true)]
        public string? InstanceType { get; set; }
        [SwaggerSchema(Description = FeatureConstants.OdsInstanceConnectionString, Nullable = false)]
        public string? ConnectionString { get; set; }

    }

    public class Validator : AbstractValidator<IAddOdsInstanceModel>
    {
        private readonly IGetOdsInstancesQuery _getOdsInstancesQuery;
        private readonly string _databaseEngine;
        public Validator(IGetOdsInstancesQuery getOdsInstancesQuery, IOptions<AppSettings> options)
        {
            _getOdsInstancesQuery = getOdsInstancesQuery;
            _databaseEngine = options.Value.DatabaseEngine ?? throw new NotFoundException<string>("AppSettings", "DatabaseEngine");

            RuleFor(m => m.Name)
                .NotEmpty()
                .Must(BeAUniqueName)
                .WithMessage(FeatureConstants.OdsInstanceAlreadyExistsMessage);

            RuleFor(m => m.InstanceType)
                .MaximumLength(100)
                .When(m => !string.IsNullOrEmpty(m.InstanceType));

            RuleFor(m => m.ConnectionString)
                .NotEmpty();

            RuleFor(m => m.ConnectionString)
                .Must(BeAValidConnectionString)
                .WithMessage(FeatureConstants.OdsInstanceConnectionStringInvalid)
                .When(m => !string.IsNullOrEmpty(m.ConnectionString));
        }

        private bool BeAUniqueName(string? name)
        {
            return _getOdsInstancesQuery.Execute().TrueForAll(x => x.Name != name);
        }

        private bool BeAValidConnectionString(string? connectionString)
        {
            return ConnectionStringHelper.ValidateConnectionString(_databaseEngine, connectionString);
        }
    }
}
