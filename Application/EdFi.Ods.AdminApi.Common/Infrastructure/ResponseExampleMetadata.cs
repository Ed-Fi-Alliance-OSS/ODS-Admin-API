// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure;

/// <summary>
/// Endpoint metadata carrying an example JSON body for a response, rendered as the
/// "application/json" example in the generated OpenAPI document.
/// </summary>
public record ResponseExampleMetadata(int StatusCode, string ExampleJson);
