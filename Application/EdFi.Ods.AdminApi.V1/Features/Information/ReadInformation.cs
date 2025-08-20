// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Infrastructure;
using EdFi.Ods.AdminApi.V1.Infrastructure.Helpers;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.V1.Features.Information;

public class ReadInformation : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("", GetInformation)
            .WithMetadata(new SwaggerOperationAttribute("Retrieve API informational metadata", null))
            .WithResponse<InformationResult>(200)
            .WithResponseCode(500, FeatureConstants.InternalServerErrorResponseDescription)
            .WithTags("Information")
            .AllowAnonymous();
    }

    internal InformationResult GetInformation()
    {
        return new InformationResult(ConstantsHelpers.Version, ConstantsHelpers.Build);
    }
}
