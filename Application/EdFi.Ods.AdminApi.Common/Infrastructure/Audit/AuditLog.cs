// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class AuditLog
{
    public long Id { get; set; }
    public AuditEventType EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public string? ClientId { get; set; }
    public string? SourceIpAddress { get; set; }
    public string? HttpVerb { get; set; }
    public string? HttpUrl { get; set; }
    public int? StatusCode { get; set; }
}
