// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using DbOdsInstanceDerivative = EdFi.Admin.DataAccess.Models.OdsInstanceDerivative;

namespace EdFi.Ods.AdminApi.V3.Features.DataStoreDerivatives;

public class DataStoreDerivativeModel { }

public static class DataStoreDerivativeMapper
{
    public static List<DataStoreDerivativeModel> ToModelList(IEnumerable<DbOdsInstanceDerivative> source)
    {
        return source.Select(_ => new DataStoreDerivativeModel()).ToList();
    }
}
