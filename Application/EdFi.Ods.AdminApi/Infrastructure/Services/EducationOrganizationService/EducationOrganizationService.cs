// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Infrastructure.Services.EducationOrganizationService;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService;

public interface IEducationOrganizationService
{
    Task Execute(string? tenantName, int? instanceId);
}

public class EducationOrganizationService(
    IOptions<AppSettings> options,
    IUsersContext usersContext,
    ISymmetricStringEncryptionProvider encryptionProvider,
    ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<EducationOrganizationService> logger)
    : EducationOrganizationServiceBase(
        options,
        usersContext,
        encryptionProvider,
        serviceScopeFactory,
        logger,
        tenantSpecificDbContextProvider.GetUsersContext,
        static (serviceProvider, tenantName) => serviceProvider.GetRequiredService<ITenantSpecificDbContextProvider>().GetAdminApiDbContext(tenantName)), IEducationOrganizationService
{
}
