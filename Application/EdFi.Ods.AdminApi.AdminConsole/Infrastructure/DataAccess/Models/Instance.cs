// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Models;

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

    public static Instance From(IInstanceRequestModel requestModel)
    {
        return new Instance
        {
            OdsInstanceId = requestModel.OdsInstanceId,
            TenantId = requestModel.TenantId,
            TenantName = requestModel.TenantName ?? string.Empty,
            InstanceName = requestModel.Name ?? string.Empty,
            InstanceType = requestModel.InstanceType,
            Credentials = requestModel.Credentials,
            Status = Enum.TryParse<InstanceStatus>(requestModel.Status, out var status)
                ? status
                : InstanceStatus.Pending,
            OdsInstanceContexts = requestModel
                .OdsInstanceContexts?.Select(s => new OdsInstanceContext
                {
                    TenantId = requestModel.TenantId,
                    ContextKey = s.ContextKey,
                    ContextValue = s.ContextValue,
                })
                .ToList(),
            OdsInstanceDerivatives = requestModel
                .OdsInstanceDerivatives?.Select(s => new OdsInstanceDerivative
                {
                    TenantId = requestModel.TenantId,
                    DerivativeType = Enum.Parse<DerivativeType>(s.DerivativeType, true),
                })
                .ToList()
        };
    }
}

public enum InstanceStatus
{
    Pending,
    Completed,
    InProgress,
    Pending_Delete,
    Deleted,
    Delete_Failed,
    Pending_Rename,
    Rename_Failed,
    Error
}
