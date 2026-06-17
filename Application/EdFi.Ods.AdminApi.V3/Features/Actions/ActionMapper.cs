// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using SecurityAction = EdFi.Security.DataAccess.Models.Action;

namespace EdFi.Ods.AdminApi.V3.Features.Actions;

public static class ActionMapper
{
    public static ActionModel ToModel(SecurityAction source)
    {
        return new ActionModel
        {
            Id = source.ActionId,
            Name = source.ActionName,
            Uri = source.ActionUri
        };
    }

    public static List<ActionModel> ToModelList(IEnumerable<SecurityAction> source)
    {
        return source.Select(ToModel).ToList();
    }
}

