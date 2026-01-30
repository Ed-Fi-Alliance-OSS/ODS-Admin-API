// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;

namespace EdFi.Ods.AdminApi.Infrastructure.Services;

public class DefaultTenantContextInitializer : IHostedService
{
    private readonly ITenantConfigurationProvider _tenantConfigProvider;
    private readonly IContextProvider<TenantConfiguration> _contextProvider;

    public DefaultTenantContextInitializer(
        ITenantConfigurationProvider tenantConfigProvider,
        IContextProvider<TenantConfiguration> contextProvider)
    {
        _tenantConfigProvider = tenantConfigProvider;
        _contextProvider = contextProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var tenants = _tenantConfigProvider.Get();
        var defaultTenant = tenants.FirstOrDefault().Value;
        if (defaultTenant != null)
        {
            _contextProvider.Set(defaultTenant);
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
