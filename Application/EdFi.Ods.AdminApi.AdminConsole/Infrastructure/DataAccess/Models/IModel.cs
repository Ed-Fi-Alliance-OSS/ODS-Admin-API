// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models
{
    public interface IModel
    {
        int? DocId { get; set; }
        int InstanceId { get; set; }
        int TenantId { get; set; }
        int? EdOrgId { get; set; }
        string Document { get; set; }
    }
}
