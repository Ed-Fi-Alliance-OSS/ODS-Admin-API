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

public class AddInstance : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder.MapPost(endpoints, "/odsInstances", Execute)
      .WithRouteOptions(b => b.WithResponseCode(201))
      .BuildForVersions(AdminApiVersions.AdminConsole);
    }

    public static async Task<IResult> Execute(InstanceValidator validator, IAddInstanceCommand addInstanceCommand, [FromBody] AddInstanceRequest request)
    {
        await validator.GuardAsync(request);
        var addedInstanceResult = await addInstanceCommand.Execute(request);

        return Results.Created($"/instances/{addedInstanceResult.Id}", addedInstanceResult);
    }

    public class AddInstanceRequest : IInstanceRequestModel
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
