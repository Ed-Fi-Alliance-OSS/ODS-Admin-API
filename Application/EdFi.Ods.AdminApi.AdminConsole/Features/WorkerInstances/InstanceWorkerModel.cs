// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;

namespace EdFi.Ods.AdminApi.AdminConsole.Features.WorkerInstances
{
    public class InstanceWorkerModel
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string TenantName { get; set; }
        public int OdsInstanceId { get; set; }
        public string InstanceName { get; set; }
        public string ResourceUrl { get; set; } = string.Empty;
        public string? oAuthUrl { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? Status { get; set; }
    }

    public class InstanceWorkerModelDTO
    {
        public string? ClientId { get; set; }
        public string? Secret { get; set; }
    }
}
