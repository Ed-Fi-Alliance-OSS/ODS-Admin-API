// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.ModelConfiguration;

public class InstanceConfiguration : IEntityTypeConfiguration<Instance>
{
    private readonly string _dbProvider;
    public InstanceConfiguration(string dbProvider)
    {
        _dbProvider = dbProvider;
    }

    public void Configure(EntityTypeBuilder<Instance> entity)
    {
        entity.ToTable("Instances", "adminconsole");

        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedOnAdd();

        entity.Property(e => e.TenantId).IsRequired();
        entity.Property(e => e.InstanceName).HasMaxLength(100);
        entity.Property(e => e.InstanceType).HasMaxLength(100);
        entity.Property(e => e.BaseUrl).HasMaxLength(250);
        entity.Property(e => e.Status).HasConversion<string>().IsRequired().HasMaxLength(50);
        entity.Property(e => e.ResourceUrl).HasMaxLength(250);
        entity.Property(e => e.OAuthUrl).HasMaxLength(250);

        switch (_dbProvider)
        {
            case DbProviders.PostgreSql:
                entity.Property(e => e.Credentials).HasColumnType("BYTEA");
                break;
            case DbProviders.SqlServer:
                entity.Property(e => e.Credentials).HasColumnType("VARBINARY(500)");
                break;
        }

        entity
        .HasMany(e => e.OdsInstanceDerivatives)
        .WithOne(e => e.Instance)
        .HasForeignKey(e => e.InstanceId)
        .IsRequired();

        entity
        .HasMany(e => e.OdsInstanceContexts)
        .WithOne(e => e.Instance)
        .HasForeignKey(e => e.InstanceId)
        .IsRequired();

        entity.HasIndex(e => e.Status);
    }
}
