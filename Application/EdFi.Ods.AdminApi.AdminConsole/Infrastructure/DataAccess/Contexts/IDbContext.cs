// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts;

public interface IDbContext
{
    DbSet<HealthCheck> HealthChecks { get; set; }

    DbSet<Instance> Instances { get; set; }

    DbSet<OdsInstanceContext> OdsInstanceContexts { get; set; }

    DbSet<OdsInstanceDerivative> OdsInstanceDerivatives { get; set; }

    DbSet<Permission> Permissions { get; set; }

    DbSet<UserProfile> UserProfiles { get; set; }

    DbSet<Step> Steps { get; set; }

    DatabaseFacade DB { get; }
    DbSet<T> Set<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    IDbContextTransaction BeginTransaction();
    void CommitTransaction();
    void RollbackTransaction();
    void DisposeTransaction();
}
