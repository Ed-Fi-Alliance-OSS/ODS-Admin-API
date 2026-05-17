// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.V3.Infrastructure.Documentation;
using FluentValidation;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores;

public class EditDataStore : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapPut(endpoints, "/dataStores/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(204))
            .BuildForVersions(AdminApiVersions.V3);
    }

    public static async Task<IResult> Handle(
        Validator validator,
        IEditDataStoreCommand editDataStoreCommand,
        ISymmetricStringEncryptionProvider encryptionProvider,
        IOptions<AppSettings> options,
        EditDataStoreRequest request,
        int id)
    {
        request.Id = id;
        await validator.GuardAsync(request);

        string encryptionKey = options.Value.EncryptionKey ?? throw new InvalidOperationException("EncryptionKey can't be null.");
        if (!string.IsNullOrEmpty(request.ConnectionString))
            request.ConnectionString = encryptionProvider.Encrypt(request.ConnectionString, Convert.FromBase64String(encryptionKey));
        else
            request.ConnectionString = string.Empty;
        editDataStoreCommand.Execute(request);
        return Results.NoContent();
    }

    [SwaggerSchema(Title = "EditDataStoreRequest")]
    public class EditDataStoreRequest : IEditDataStoreModel
    {
        [SwaggerSchema(Description = FeatureConstants.DataStoreName, Nullable = false)]
        public string? Name { get; set; }
        [SwaggerSchema(Description = FeatureConstants.DataStoreTypeDescription, Nullable = true)]
        public string? DataStoreType { get; set; }
        [SwaggerSchema(Description = FeatureConstants.DataStoreConnectionString, Nullable = true)]
        public string? ConnectionString { get; set; }
        [SwaggerExclude]
        public int Id { get; set; }
    }

    public class Validator : AbstractValidator<IEditDataStoreModel>
    {
        private readonly IGetDataStoresQuery _getDataStoresQuery;
        private readonly IGetDataStoreQuery _getDataStoreQuery;
        private readonly string _databaseEngine;

        public Validator(IGetDataStoresQuery getDataStoresQuery, IGetDataStoreQuery getDataStoreQuery, IOptions<AppSettings> options)
        {
            _getDataStoresQuery = getDataStoresQuery;
            _getDataStoreQuery = getDataStoreQuery;
            _databaseEngine = options.Value.DatabaseEngine ?? throw new NotFoundException<string>("AppSettings", "DatabaseEngine");

            RuleFor(m => m.Name)
                .NotEmpty()
                .Must(BeAUniqueName)
                .WithMessage(FeatureConstants.ClaimSetAlreadyExistsMessage)
                .When(m => BeAnExistingDataStore(m.Id) && NameIsChanged(m));

            RuleFor(m => m.DataStoreType)
                .MaximumLength(100)
                .When(m => !string.IsNullOrEmpty(m.DataStoreType));

            RuleFor(m => m.ConnectionString)
                .Must(BeAValidConnectionString)
                .WithMessage(FeatureConstants.DataStoreConnectionStringInvalid)
                .When(m => !string.IsNullOrEmpty(m.ConnectionString));
        }

        private bool BeAnExistingDataStore(int id)
        {
            _getDataStoreQuery.Execute(id);
            return true;
        }

        private bool NameIsChanged(IEditDataStoreModel model)
        {
            return _getDataStoreQuery.Execute(model.Id).Name != model.Name;
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
