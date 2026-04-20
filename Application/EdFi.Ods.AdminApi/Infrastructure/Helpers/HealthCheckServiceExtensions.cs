// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Infrastructure;

public static class HealthCheckServiceExtensions
{
    public static IServiceCollection AddHealthCheck(
        this IServiceCollection services,
        IConfigurationRoot configuration
    )
    {
        return EdFi.Ods.AdminApi.Common.Infrastructure.HealthCheckServiceExtensions.AddHealthCheck(services, configuration);
    }
}
