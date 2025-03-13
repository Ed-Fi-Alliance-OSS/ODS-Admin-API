// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Tenants;
using EdFi.Ods.AdminApi.Common.Settings;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services;

public class TenantBackgroundService : BackgroundService
{
    private readonly IDisposable _optionsChangedListener;
    private static readonly ILog _log = LogManager.GetLogger(typeof(TenantService));
    private readonly IServiceScopeFactory _serviceScopeFactory;
    public TenantBackgroundService(IOptionsMonitor<AppSettingsFile> optionsMonitor, IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _optionsChangedListener = optionsMonitor.OnChange(async (opt) => await OnAppSettingsChangedAsync())!;
    }

    private async Task OnAppSettingsChangedAsync()
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        _log.Info("The appsettings file has been modified");

        IAdminConsoleTenantsService scopedProcessingService =
            scope.ServiceProvider.GetRequiredService<IAdminConsoleTenantsService>();

        await scopedProcessingService.InitializeTenantsAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _log.Info("Stopping background");
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _optionsChangedListener.Dispose();
        base.Dispose();
    }
}
