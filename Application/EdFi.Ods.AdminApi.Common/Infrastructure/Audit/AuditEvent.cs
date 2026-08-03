// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class AuditEvent
{
    public required string AdminConnectionString { get; init; }
    public required AuditEventType EventType { get; init; }
    public required DateTime Timestamp { get; init; }
    public string? ClientId { get; init; }
    public string? SourceIpAddress { get; init; }
    public string? HttpVerb { get; init; }
    public string? HttpUrl { get; init; }
    public int? StatusCode { get; init; }
}
