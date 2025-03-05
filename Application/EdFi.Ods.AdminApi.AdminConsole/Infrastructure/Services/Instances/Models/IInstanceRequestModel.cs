// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Models
{
    public interface IInstanceRequestModel
    {
        int Id { get; set; }
        int OdsInstanceId { get; }
        int TenantId { get; }
        string TenantName { get; }
        string Name { get; }
        string? InstanceType { get; }
        ICollection<OdsInstanceContextModel>? OdsInstanceContexts { get; }
        ICollection<OdsInstanceDerivativeModel>? OdsInstanceDerivatives { get; }

        byte[]? Credentials { get; }
        public string? Status { get; set; }
    }

    public class OdsInstanceContextModel
    {
        public string ContextKey { get; set; } = string.Empty;
        public string ContextValue { get; set; } = string.Empty;
    }

    public class OdsInstanceDerivativeModel
    {
        public string DerivativeType { get; set; } = string.Empty;
    }
}
