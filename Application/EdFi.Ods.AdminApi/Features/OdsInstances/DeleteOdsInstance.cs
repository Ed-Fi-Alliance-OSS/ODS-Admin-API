// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using FluentValidation;

namespace EdFi.Ods.AdminApi.Features.OdsInstances;

public class DeleteOdsInstance : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapDelete(endpoints, "/odsInstances/{id}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(200, FeatureCommonConstants.DeletedSuccessResponseDescription))
            .BuildForVersions(AdminApiVersions.V2);
    }

    internal async Task<IResult> Handle(IDeleteOdsInstanceCommand deleteOdsInstanceCommand, Validator validator, int id)
    {
        var request = new Request() { Id = id };
        await validator.GuardAsync(request);
        deleteOdsInstanceCommand.Execute(request.Id);
        return await Task.FromResult(Results.Ok("Ods Instance".ToJsonObjectResponseDeleted()));
    }

    public class Validator : AbstractValidator<Request>
    {
        private readonly IGetOdsInstanceQuery _getOdsInstanceQuery;
        private readonly IGetApplicationsByOdsInstanceIdQuery _getApplicationByOdsInstanceIdQuery;
        private OdsInstance? OdsInstanceEntity = null;
        public Validator(IGetOdsInstanceQuery getOdsInstanceQuery, IGetApplicationsByOdsInstanceIdQuery getApplicationByOdsInstanceIdQuery)
        {
            _getOdsInstanceQuery = getOdsInstanceQuery;
            _getApplicationByOdsInstanceIdQuery = getApplicationByOdsInstanceIdQuery;

            RuleFor(m => m.Id)
                .Must(NotHaveApplicationsRelationships)
                .WithMessage(FeatureConstants.OdsInstanceCantBeDeletedMessage)
                .When(Exist);
            RuleFor(m => m.Id)
                .Must(NotHaveOdsInstanceContextsRelationships)
                .WithMessage(FeatureConstants.OdsInstanceCantBeDeletedMessage)
                .When(Exist);
            RuleFor(m => m.Id)
                .Must(NotHaveOdsInstanceDerivativesRelationships)
                .WithMessage(FeatureConstants.OdsInstanceCantBeDeletedMessage)
                .When(Exist);
        }

        private bool Exist(Request request)
        {
            OdsInstanceEntity = _getOdsInstanceQuery.Execute(request.Id);
            return true;
        }
        private bool NotHaveApplicationsRelationships<T>(Request model, int odsIntanceId, ValidationContext<T> context)
        {
            context.MessageFormatter.AppendArgument("Table", "Applications");
            List<Application> appList = _getApplicationByOdsInstanceIdQuery.Execute(odsIntanceId) ?? [];
            return appList.Count == 0;
        }

        private bool NotHaveOdsInstanceContextsRelationships<T>(Request model, int odsIntanceId, ValidationContext<T> context)
        {
            context.MessageFormatter.AppendArgument("Table", "OdsInstanceContexts");
            return OdsInstanceEntity!.OdsInstanceContexts.Count == 0;
        }

        private bool NotHaveOdsInstanceDerivativesRelationships<T>(Request model, int odsIntanceId, ValidationContext<T> context)
        {
            context.MessageFormatter.AppendArgument("Table", "OdsInstanceDerivatives");
            return OdsInstanceEntity!.OdsInstanceDerivatives.Count == 0;
        }
    }

    public class Request
    {
        public int Id { get; set; }
    }
}
