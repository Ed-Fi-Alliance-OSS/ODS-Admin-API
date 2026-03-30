// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using DbOdsInstanceContext = EdFi.Admin.DataAccess.Models.OdsInstanceContext;

namespace EdFi.Ods.AdminApi.Features.OdsInstanceContext;

public static class OdsInstanceContextMapper
{
    public static OdsInstanceContextModel ToModel(DbOdsInstanceContext source)
    {
        return new OdsInstanceContextModel
        {
            OdsInstanceContextId = source.OdsInstanceContextId,
            OdsInstanceId = source.OdsInstance?.OdsInstanceId ?? 0,
            ContextKey = source.ContextKey,
            ContextValue = source.ContextValue
        };
    }

    public static List<OdsInstanceContextModel> ToModelList(IEnumerable<DbOdsInstanceContext> source)
    {
        return source.Select(ToModel).ToList();
    }
}
