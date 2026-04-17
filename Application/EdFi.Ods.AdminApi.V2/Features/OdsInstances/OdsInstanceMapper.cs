// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V2.Features.OdsInstanceContext;
using EdFi.Ods.AdminApi.V2.Features.OdsInstanceDerivative;

namespace EdFi.Ods.AdminApi.V2.Features.OdsInstances;

public static class OdsInstanceMapper
{
    public static OdsInstanceModel ToModel(OdsInstance source)
    {
        return new OdsInstanceModel
        {
            OdsInstanceId = source.OdsInstanceId,
            Name = source.Name,
            InstanceType = source.InstanceType
        };
    }

    public static OdsInstanceDetailModel ToDetailModel(OdsInstance source)
    {
        return new OdsInstanceDetailModel
        {
            OdsInstanceId = source.OdsInstanceId,
            Name = source.Name,
            InstanceType = source.InstanceType,
            OdsInstanceContexts = OdsInstanceContextMapper.ToModelList(source.OdsInstanceContexts),
            OdsInstanceDerivatives = OdsInstanceDerivativeMapper.ToModelList(source.OdsInstanceDerivatives)
        };
    }

    public static List<OdsInstanceModel> ToModelList(IEnumerable<OdsInstance> source)
    {
        return source.Select(ToModel).ToList();
    }
}
