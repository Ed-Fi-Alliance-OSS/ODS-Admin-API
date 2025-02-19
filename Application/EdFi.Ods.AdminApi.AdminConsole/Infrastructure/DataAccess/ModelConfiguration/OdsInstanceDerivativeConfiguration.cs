// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.ModelConfiguration;

public class OdsInstanceDerivativeConfiguration : IEntityTypeConfiguration<OdsInstanceDerivative>
{
    public void Configure(EntityTypeBuilder<OdsInstanceDerivative> entity)
    {
        entity.ToTable("OdsInstanceDerivatives", "adminconsole");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id).ValueGeneratedOnAdd();
        entity.Property(e => e.TenantId).IsRequired();
        entity.Property(e => e.DerivativeType).HasConversion<string>().HasMaxLength(50).IsRequired();
    }
}
