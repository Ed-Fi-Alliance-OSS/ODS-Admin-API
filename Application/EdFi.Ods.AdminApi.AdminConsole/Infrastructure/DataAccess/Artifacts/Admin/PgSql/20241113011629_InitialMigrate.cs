// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Artifacts.PgSql
{
    /// <inheritdoc />
    public partial class InitialMigrate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "adminconsole");

            migrationBuilder.CreateTable(
                name: "HealthChecks",
                schema: "adminconsole",
                columns: table => new
                {
                    DocId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstanceId = table.Column<int>(type: "integer", nullable: false),
                    EdOrgId = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Document = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthChecks", x => x.DocId);
                });

            migrationBuilder.CreateTable(
                name: "Instances",
                schema: "adminconsole",
                columns: table => new
                {
                    DocId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstanceId = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    EdOrgId = table.Column<int>(type: "integer", nullable: true),
                    Document = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instances", x => x.DocId);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "adminconsole",
                columns: table => new
                {
                    DocId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstanceId = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    EdOrgId = table.Column<int>(type: "integer", nullable: true),
                    Document = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.DocId);
                });

            migrationBuilder.CreateTable(
                name: "UserProfile",
                schema: "adminconsole",
                columns: table => new
                {
                    DocId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstanceId = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    EdOrgId = table.Column<int>(type: "integer", nullable: true),
                    Document = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfile", x => x.DocId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthChecks_EdOrgId",
                schema: "adminconsole",
                table: "HealthChecks",
                column: "EdOrgId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthChecks_InstanceId",
                schema: "adminconsole",
                table: "HealthChecks",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthChecks_TenantId",
                schema: "adminconsole",
                table: "HealthChecks",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instances_EdOrgId",
                schema: "adminconsole",
                table: "Instances",
                column: "EdOrgId");

            migrationBuilder.CreateIndex(
                name: "IX_Instances_InstanceId",
                schema: "adminconsole",
                table: "Instances",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_EdOrgId",
                schema: "adminconsole",
                table: "Permissions",
                column: "EdOrgId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_InstanceId",
                schema: "adminconsole",
                table: "Permissions",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_EdOrgId",
                schema: "adminconsole",
                table: "UserProfile",
                column: "EdOrgId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfile_InstanceId",
                schema: "adminconsole",
                table: "UserProfile",
                column: "InstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HealthChecks",
                schema: "adminconsole");

            migrationBuilder.DropTable(
                name: "Instances",
                schema: "adminconsole");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "adminconsole");

            migrationBuilder.DropTable(
                name: "UserProfile",
                schema: "adminconsole");
        }
    }
}
