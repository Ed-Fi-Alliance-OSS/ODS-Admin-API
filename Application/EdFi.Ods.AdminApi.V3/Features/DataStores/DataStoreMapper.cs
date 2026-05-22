// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V3.Features.DataStoreContexts;
using EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;

namespace EdFi.Ods.AdminApi.V3.Features.DataStores;

public static class DataStoreMapper
{
    public static DataStoreModel ToModel(OdsInstance source)
    {
        return new DataStoreModel
        {
            DataStoreId = source.OdsInstanceId,
            Name = source.Name,
            DataStoreType = source.InstanceType
        };
    }

    public static DataStoreDetailModel ToDetailModel(OdsInstance source)
    {
        return new DataStoreDetailModel
        {
            DataStoreId = source.OdsInstanceId,
            Name = source.Name,
            DataStoreType = source.InstanceType,
            DataStoreContexts = DataStoreContextMapper.ToModelList(source.OdsInstanceContexts),
            DataStoreDerivatives = DataStoreDerivativeMapper.ToModelList(source.OdsInstanceDerivatives)
        };
    }

    public static List<DataStoreModel> ToModelList(IEnumerable<OdsInstance> source)
    {
        return source.Select(ToModel).ToList();
    }
}
