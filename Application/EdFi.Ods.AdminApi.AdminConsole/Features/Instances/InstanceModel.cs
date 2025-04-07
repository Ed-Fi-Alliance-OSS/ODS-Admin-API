// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.AdminConsole.Features.Instances;

public class InstanceModel
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InstanceType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public List<OdsInstanceContextForInstanceModel>? OdsInstanceContexts { get; set; }
    public List<OdsInstanceDerivativeForInstanceModel>? OdsInstanceDerivatives { get; set; }
}

public class OdsInstanceContextForInstanceModel
{
    public int Id { get; set; }
    public int InstanceId { get; set; }
    public string ContextKey { get; set; } = string.Empty;
    public string ContextValue { get; set; } = string.Empty;
}

public class OdsInstanceDerivativeForInstanceModel
{
    public int Id { get; set; }
    public int InstanceId { get; set; }
    public string DerivativeType { get; set; } = string.Empty;
}
