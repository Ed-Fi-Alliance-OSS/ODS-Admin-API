// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;

namespace EdFi.Ods.AdminApi.Features.Tenants;

public static class TenantMapper
{
    public static TenantOdsInstanceModel ToOdsInstanceModel(OdsInstance source)
    {
        return new TenantOdsInstanceModel
        {
            OdsInstanceId = source.OdsInstanceId,
            Name = source.Name,
            InstanceType = source.InstanceType,
        };
    }

    public static List<TenantOdsInstanceModel> ToOdsInstanceModelList(IEnumerable<OdsInstance> source)
    {
        return source.Select(ToOdsInstanceModel).ToList();
    }
}