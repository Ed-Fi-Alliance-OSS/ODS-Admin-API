// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using DbOdsInstanceDerivative = EdFi.Admin.DataAccess.Models.OdsInstanceDerivative;

namespace EdFi.Ods.AdminApi.V3.Features.OdsInstanceDerivative;

public static class OdsInstanceDerivativeMapper
{
    public static OdsInstanceDerivativeModel ToModel(DbOdsInstanceDerivative source)
    {
        return new OdsInstanceDerivativeModel
        {
            Id = source.OdsInstanceDerivativeId,
            OdsInstanceId = source.OdsInstance?.OdsInstanceId,
            DerivativeType = source.DerivativeType
        };
    }

    public static List<OdsInstanceDerivativeModel> ToModelList(IEnumerable<DbOdsInstanceDerivative> source)
    {
        return source.Select(ToModel).ToList();
    }
}

