// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Extensions;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.Tenants;

public class DeleteTenant : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapDelete(endpoints, "/tenants/{tenantName}", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(200, FeatureCommonConstants.DeletedSuccessResponseDescription))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> Handle(
        [FromServices] ITenantsService iTenantsService,
        DeleteTenantCommand deleteTenantCommand,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<AppSettings> options,
        string tenantName)
    {
        if (!options.Value.MultiTenancy)
            return Results.BadRequest(new { message = "Not multitenant environment." });

        var tenant = await iTenantsService.GetTenantByTenantIdAsync(tenantName);
        if (tenant == null)
            return Results.NotFound();

        deleteTenantCommand.Execute(tenantName);

        await InitializeTenantsAsync(serviceScopeFactory);

        return Results.Ok("Tenant".ToJsonObjectResponseDeleted());
    }

    [SwaggerSchema(Title = "DeleteTenantRequest")]
    public class DeleteTenantRequest
    {
        [SwaggerSchema(Description = "The unique name of the tenant to delete.", Nullable = false)]
        public string TenantName { get; set; } = string.Empty;
    }

    private static async Task InitializeTenantsAsync(IServiceScopeFactory serviceScopeFactory)
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();

        ITenantsService scopedProcessingService =
            scope.ServiceProvider.GetRequiredService<ITenantsService>();

        await scopedProcessingService.InitializeTenantsAsync();
    }
}
