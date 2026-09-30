// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Common.Features;

[SwaggerSchema(Title = "JobQueuedResult", Description = "Response returned when a request has been queued as a background job")]
public class JobQueuedResult
{
    public string JobId { get; set; } = null!;
    public string Message { get; set; } = null!;
}
