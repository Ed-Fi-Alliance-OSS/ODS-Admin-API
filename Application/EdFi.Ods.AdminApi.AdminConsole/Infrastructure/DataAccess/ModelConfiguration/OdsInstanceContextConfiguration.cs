// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.ModelConfiguration;

public class OdsInstanceContextConfiguration : IEntityTypeConfiguration<OdsInstanceContext>
{
    public void Configure(EntityTypeBuilder<OdsInstanceContext> entity)
    {
        entity.ToTable("OdsInstanceContexts", "adminconsole");

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedOnAdd();
        entity.Property(e => e.TenantId).IsRequired();
        entity.Property(e => e.ContextKey).HasMaxLength(50).IsRequired();
        entity.Property(e => e.ContextValue).HasMaxLength(50);
    }
}
