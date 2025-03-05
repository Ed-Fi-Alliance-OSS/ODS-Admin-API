// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Text.Json.Serialization;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Validator;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace EdFi.Ods.AdminApi.AdminConsole.Features.Instances;

public class EditInstance : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPut(endpoints, "/odsInstances/{id}", Execute)
            .WithRouteOptions(b => b.WithResponseCode(204))
            .BuildForVersions(AdminApiVersions.AdminConsole);
    }

    public async Task<IResult> Execute(InstanceValidator validator, IEditInstanceCommand editInstanceCommand, [FromBody] EditInstanceRequest request, int id)
    {
        request.Id = id;
        await validator.GuardAsync(request);
        var instance = await editInstanceCommand.Execute(id, request);
        return Results.NoContent();
    }

    public class EditInstanceRequest : IInstanceRequestModel
    {
        public int TenantId { get; set; }

        public string TenantName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? InstanceType { get; set; }

        public ICollection<OdsInstanceContextModel>? OdsInstanceContexts { get; set; }

        public ICollection<OdsInstanceDerivativeModel>? OdsInstanceDerivatives { get; set; }

        [JsonIgnore]
        public byte[]? Credentials { get; set; }

        [JsonIgnore]
        public string? Status { get; set; }

        [JsonIgnore]
        public int OdsInstanceId { get; set; }

        [JsonIgnore]
        public int Id { get; set; }
    }
}
