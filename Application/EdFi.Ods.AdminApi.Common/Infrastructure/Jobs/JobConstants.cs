// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;

public class JobConstants
{
    public const string JobTypeKey = "JobType";
    public const string TenantNameKey = "TenantName";
    public const string DbInstanceIdKey = "DbInstanceId";
    public const string OdsInstanceIdKey = "OdsInstanceId";
    public const string CreateInstanceJobName = "CreateInstanceJob";
    public const string CreatePendingDbInstancesDispatcherJobName = "CreatePendingDbInstancesDispatcherJob";
    public const string DeleteInstanceJobName = "DeleteInstanceJob";
    public const string DeletePendingDbInstancesDispatcherJobName = "DeletePendingDbInstancesDispatcherJob";
    public const string RefreshEducationOrganizationsJobName = "RefreshEducationOrganizationsJob";
}
