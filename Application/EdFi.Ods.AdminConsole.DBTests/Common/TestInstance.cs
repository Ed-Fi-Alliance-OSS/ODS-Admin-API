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
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Models;

namespace EdFi.Ods.AdminConsole.DBTests.Common
{
    public class TestInstance : IInstanceRequestModel
    {
        public int OdsInstanceId { get; set; }
        public int TenantId { get; set; }
        public string TenantName { get; set; }
        public string Name { get; set; }
        public string InstanceType { get; set; }
        public ICollection<OdsInstanceContextModel> OdsInstanceContexts { get; set; }
        public ICollection<OdsInstanceDerivativeModel> OdsInstanceDerivatives { get; set; }

        [JsonIgnore]
        public byte[] Credentials { get; set; }

        public string Status { get; set; }

        [JsonIgnore]
        public int Id { get; set; }
    }
}
