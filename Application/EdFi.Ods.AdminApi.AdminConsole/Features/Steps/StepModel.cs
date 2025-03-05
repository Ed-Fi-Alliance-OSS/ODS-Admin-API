// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Newtonsoft.Json;

namespace EdFi.Ods.AdminApi.AdminConsole.Features.Steps;

public class StepModel
{
    [JsonProperty("number")]
    public int? Number { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("startedAt")]
    public DateTime? StartedAt { get; set; }

    [JsonProperty("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }
}
