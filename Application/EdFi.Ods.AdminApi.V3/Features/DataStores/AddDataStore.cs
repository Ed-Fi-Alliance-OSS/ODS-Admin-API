// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FluentValidation;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores;

public class AddDataStore : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
           .MapPost(endpoints, "/dataStores", Handle)
           .WithDefaultSummaryAndDescription()
           .WithRouteOptions(b => b.WithResponseCode(201))
           .BuildForVersions(AdminApiVersions.V3);
    }

    public static async Task<IResult> Handle(
        Validator validator,
        IAddDataStoreCommand addDataStoreCommand,
        ISymmetricStringEncryptionProvider encryptionProvider,
        IOptions<AppSettings> options,
        AddDataStoreRequest request,
        HttpContext httpContext)
    {
        await validator.GuardAsync(request);
        string encryptionKey = options.Value.EncryptionKey ?? throw new InvalidOperationException("EncryptionKey can't be null.");
        request.ConnectionString = encryptionProvider.Encrypt(request.ConnectionString, Convert.FromBase64String(encryptionKey));
        var added = addDataStoreCommand.Execute(request);
        var absoluteLocation = ResourceUrlHelper.BuildAbsoluteResourceUrl(httpContext, AdminApiMode.V3, $"/dataStores/{added.OdsInstanceId}");
        return Results.Created(absoluteLocation, null);
    }

    [SwaggerSchema(Title = "AddDataStoreRequest")]
    public class AddDataStoreRequest : IAddDataStoreModel
    {
        [SwaggerSchema(Description = FeatureConstants.DataStoreName, Nullable = false)]
        public string? Name { get; set; }
        [SwaggerSchema(Description = FeatureConstants.DataStoreTypeDescription, Nullable = true)]
        public string? DataStoreType { get; set; }
        [SwaggerSchema(Description = FeatureConstants.DataStoreConnectionString, Nullable = false)]
        public string? ConnectionString { get; set; }
    }

    public class Validator : AbstractValidator<IAddDataStoreModel>
    {
        private readonly IGetDataStoresQuery _getDataStoresQuery;
        private readonly string _databaseEngine;

        public Validator(IGetDataStoresQuery getDataStoresQuery, IOptions<AppSettings> options)
        {
            _getDataStoresQuery = getDataStoresQuery;
            _databaseEngine = options.Value.DatabaseEngine ?? throw new NotFoundException<string>("AppSettings", "DatabaseEngine");

            RuleFor(m => m.Name)
                .NotEmpty()
                .Must(BeAUniqueName)
                .WithMessage(FeatureConstants.DataStoreAlreadyExistsMessage);

            RuleFor(m => m.DataStoreType)
                .MaximumLength(100)
                .When(m => !string.IsNullOrEmpty(m.DataStoreType));

            RuleFor(m => m.ConnectionString)
                .NotEmpty();

            RuleFor(m => m.ConnectionString)
                .Must(BeAValidConnectionString)
                .WithMessage(FeatureConstants.DataStoreConnectionStringInvalid)
                .When(m => !string.IsNullOrEmpty(m.ConnectionString));
        }

        private bool BeAUniqueName(string? name)
        {
            return _getDataStoresQuery.Execute().TrueForAll(x => x.Name != name);
        }

        private bool BeAValidConnectionString(string? connectionString)
        {
            return ConnectionStringHelper.ValidateConnectionString(_databaseEngine, connectionString);
        }
    }
}
