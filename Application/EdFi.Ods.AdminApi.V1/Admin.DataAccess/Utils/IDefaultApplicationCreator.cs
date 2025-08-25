// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Admin.DataAccess;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;

namespace EdFi.Admin.DataAccess.V1.Utils
{
    public interface IDefaultApplicationCreator
    {
        Application FindOrCreateUpdatedDefaultSandboxApplication(int vendorId, SandboxType sandboxType);

        void AddEdOrgIdsToApplication(IList<int> edOrgIds, int applicationId);
    }
}
