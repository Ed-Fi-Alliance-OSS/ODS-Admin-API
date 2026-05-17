// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using FluentValidation;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores;

public class DeleteDataStore : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapDelete(endpoints, "/dataStores/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(204))
            .BuildForVersions(AdminApiVersions.V3);
    }

    internal async Task<IResult> Handle(IDeleteDataStoreCommand deleteDataStoreCommand, Validator validator, int id)
    {
        var request = new Request { Id = id };
        await validator.GuardAsync(request);
        deleteDataStoreCommand.Execute(request.Id);
        return await Task.FromResult(Results.NoContent());
    }

    public class Validator : AbstractValidator<Request>
    {
        private readonly IGetDataStoreQuery _getDataStoreQuery;
        private readonly IGetApplicationsByDataStoreIdQuery _getApplicationsByDataStoreIdQuery;
        private OdsInstance? _dataStoreEntity = null;

        public Validator(IGetDataStoreQuery getDataStoreQuery, IGetApplicationsByDataStoreIdQuery getApplicationsByDataStoreIdQuery)
        {
            _getDataStoreQuery = getDataStoreQuery;
            _getApplicationsByDataStoreIdQuery = getApplicationsByDataStoreIdQuery;

            RuleFor(m => m.Id)
                .Must(NotHaveApplicationsRelationships)
                .WithMessage(FeatureConstants.DataStoreCantBeDeletedMessage)
                .When(Exist);
            RuleFor(m => m.Id)
                .Must(NotHaveDataStoreContextsRelationships)
                .WithMessage(FeatureConstants.DataStoreCantBeDeletedMessage)
                .When(Exist);
            RuleFor(m => m.Id)
                .Must(NotHaveDataStoreDerivativesRelationships)
                .WithMessage(FeatureConstants.DataStoreCantBeDeletedMessage)
                .When(Exist);
        }

        private bool Exist(Request request)
        {
            _dataStoreEntity = _getDataStoreQuery.Execute(request.Id);
            return true;
        }

        private bool NotHaveApplicationsRelationships<T>(Request model, int id, ValidationContext<T> context)
        {
            context.MessageFormatter.AppendArgument("Table", "Applications");
            List<Application> appList = _getApplicationsByDataStoreIdQuery.Execute(id) ?? [];
            return appList.Count == 0;
        }

        private bool NotHaveDataStoreContextsRelationships<T>(Request model, int id, ValidationContext<T> context)
        {
            context.MessageFormatter.AppendArgument("Table", "DataStoreContexts");
            return _dataStoreEntity!.OdsInstanceContexts.Count == 0;
        }

        private bool NotHaveDataStoreDerivativesRelationships<T>(Request model, int id, ValidationContext<T> context)
        {
            context.MessageFormatter.AppendArgument("Table", "DataStoreDerivatives");
            return _dataStoreEntity!.OdsInstanceDerivatives.Count == 0;
        }
    }

    public class Request
    {
        public int Id { get; set; }
    }
}
