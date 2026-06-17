// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;

namespace EdFi.Ods.AdminApi.V3.Features.Tenants;

public static class TenantMapper
{
    public static TenantDataStoreModel ToDataStoreModel(OdsInstance source)
    {
        return new TenantDataStoreModel
        {
            DataStoreId = source.OdsInstanceId,
            Name = source.Name,
            DataStoreType = source.InstanceType,
        };
    }

    public static List<TenantDataStoreModel> ToDataStoreModelList(IEnumerable<OdsInstance> source)
    {
        return source.Select(ToDataStoreModel).ToList();
    }
}
