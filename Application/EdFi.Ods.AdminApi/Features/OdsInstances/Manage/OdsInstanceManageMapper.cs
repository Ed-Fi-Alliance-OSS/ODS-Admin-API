// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Models;

namespace EdFi.Ods.AdminApi.Features.OdsInstances.Manage;

public static class OdsInstanceManageMapper
{
    public static OdsInstanceManageModel ToModel(OdsInstanceManage source)
    {
        return new OdsInstanceManageModel
        {
            Id = source.Id,
            Name = source.Name,
            OdsInstanceId = source.OdsInstanceId,
            OdsInstanceName = source.OdsInstanceName,
            Status = source.Status,
            DatabaseTemplate = source.DatabaseTemplate,
            DatabaseName = source.DatabaseName,
            LastRefreshed = source.LastRefreshed,
            LastModifiedDate = source.LastModifiedDate,
        };
    }

    public static List<OdsInstanceManageModel> ToModelList(IEnumerable<OdsInstanceManage> source)
    {
        return source.Select(ToModel).ToList();
    }
}
