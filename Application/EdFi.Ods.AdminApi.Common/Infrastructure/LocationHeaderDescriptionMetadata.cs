// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure;

/// <summary>
/// Endpoint metadata overriding the OpenAPI description of the "Location" header
/// documented on a 201 response. Used for endpoints where Location does not point
/// at the resource that was created (e.g. a queued job's status endpoint).
/// </summary>
public class LocationHeaderDescriptionMetadata(string description)
{
    public string Description { get; } = description;
}
