// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Threading.Tasks;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Permissions.Commands;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Permissions.Queries;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminConsole.DBTests.Database.CommandTests;

[TestFixture]
public class GetPermissionByIdQueryTests : PlatformUsersContextTestBase
{
    private IOptions<AppSettings> _options { get; set; }

    [OneTimeSetUp]
    public virtual async Task FixtureSetup()
    {
        AdminConsoleSettings appSettings = new AdminConsoleSettings();
        await Task.Yield();
    }

    [Test]
    public void ShouldExecute()
    {
        var permissionDocument = "{\"data\":[],\"type\":\"Response\"}";
        Permission result = null;

        var newPermission = new TestPermission
        {
            InstanceId = 1,
            TenantId = 1,
            EdOrgId = 1,
            Document = permissionDocument
        };

        Transaction(async dbContext =>
        {
            var repository = new CommandRepository<Permission>(dbContext);
            var command = new AddPermissionCommand(repository);

            result = await command.Execute(newPermission);
        });

        Transaction(async dbContext =>
        {
            var repository = new QueriesRepository<Permission>(dbContext);
            var query = new GetPermissionByIdQuery(repository);
            var permission = await query.Execute(result.TenantId, result.DocId.Value);

            permission.DocId.ShouldBe(result.DocId);
            permission.TenantId.ShouldBe(newPermission.TenantId);
            permission.InstanceId.ShouldBe(newPermission.InstanceId);
            permission.EdOrgId.ShouldBe(newPermission.EdOrgId);
            permission.Document.ShouldBe(newPermission.Document);
        });
    }

    private class TestPermission : IAddPermissionModel
    {
        public int DocId { get; }
        public int TenantId { get; set; }
        public int InstanceId { get; set; }
        public int? EdOrgId { get; set; }
        public string Document { get; set; }
    }
}
