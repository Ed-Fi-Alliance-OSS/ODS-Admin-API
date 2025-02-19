// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Models
{
    public interface IInstanceRequestModel
    {
        int Id { get; set; }
        int OdsInstanceId { get; }
        int TenantId { get; }
        string? Name { get; }
        string? InstanceType { get; }
        ICollection<OdsInstanceContextModel>? OdsInstanceContexts { get; }
        ICollection<OdsInstanceDerivativeModel>? OdsInstanceDerivatives { get; }

        byte[]? Credetials { get; }
        public string? Status { get; set; }
    }

    public class OdsInstanceContextModel
    {
        public string ContextKey { get; set; }
        public string ContextValue { get; set; }
    }

    public class OdsInstanceDerivativeModel
    {
        public string DerivativeType { get; set; }
    }
}
