// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores.Manage;

public static class DataStoreManageMapper
{
    public static DataStoreManageModel ToModel(OdsInstanceManage source)
    {
        return new DataStoreManageModel
        {
            Id = source.Id,
            Name = source.Name,
            DataStoreId = source.OdsInstanceId,
            DataStoreName = source.OdsInstanceName,
            Status = source.Status,
            DatabaseTemplate = source.DatabaseTemplate,
            DatabaseName = source.DatabaseName,
            LastRefreshed = source.LastRefreshed,
            LastModifiedDate = source.LastModifiedDate,
        };
    }

    public static List<DataStoreManageModel> ToModelList(IEnumerable<OdsInstanceManage> source)
    {
        return source.Select(ToModel).ToList();
    }
}
