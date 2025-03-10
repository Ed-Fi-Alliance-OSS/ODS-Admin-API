// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;

public class Instance
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public int? OdsInstanceId { get; set; }
    public string InstanceName { get; set; } = string.Empty;
    public string? InstanceType { get; set; }
    public string? BaseUrl { get; set; }
    public InstanceStatus Status { get; set; } = InstanceStatus.Pending;
    public string? ResourceUrl { get; set; }
    public string? OAuthUrl { get; set; }
    public byte[]? Credentials { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<OdsInstanceContext>? OdsInstanceContexts { get; set; }
    public ICollection<OdsInstanceDerivative>? OdsInstanceDerivatives { get; set; }
}

public enum InstanceStatus
{
    Pending,
    Completed,
    InProgress,
    Pending_Delete,
    Deleted,
    Delete_Failed,
    Error
}

