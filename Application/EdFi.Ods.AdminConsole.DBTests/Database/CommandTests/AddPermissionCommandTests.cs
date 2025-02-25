// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Permissions.Commands;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminConsole.DBTests.Database.CommandTests;

[TestFixture]
public class AddPermissionCommandTests : PlatformUsersContextTestBase
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

        var aes256SymmetricStringEncryptionProvider = new Aes256SymmetricStringEncryptionProvider();
        var encryptionKey = Testing.GetEncryptionKeyResolver().GetEncryptionKey();

        Transaction(async dbContext =>
        {
            var repository = new CommandRepository<Permission>(dbContext);
            var newPermission = new TestPermission
            {
                InstanceId = 1,
                TenantId = 1,
                EdOrgId = 1,
                Document = permissionDocument
            };

            var command = new AddPermissionCommand(repository);

            var result = await command.Execute(newPermission);
        });

        Transaction(dbContext =>
        {
            var persistedPermission = dbContext.Permissions;
            persistedPermission.Count().ShouldBe(1);
            persistedPermission.First().DocId.ShouldBe(1);
            persistedPermission.First().TenantId.ShouldBe(1);
            persistedPermission.First().InstanceId.ShouldBe(1);
            persistedPermission.First().EdOrgId.ShouldBe(1);

            JsonNode jnDocument = JsonNode.Parse(persistedPermission.First().Document);

            var encryptedClientId = jnDocument!["clientId"]?.AsValue().ToString();
            var encryptedClientSecret = jnDocument!["clientSecret"]?.AsValue().ToString();

            var clientId = "CLIENT321";
            var clientSecret = "SECRET456";

            if (!string.IsNullOrEmpty(encryptedClientId) && !string.IsNullOrEmpty(encryptedClientSecret))
            {
                aes256SymmetricStringEncryptionProvider.TryDecrypt(encryptedClientId, Convert.FromBase64String(encryptionKey), out clientId);
                aes256SymmetricStringEncryptionProvider.TryDecrypt(encryptedClientSecret, Convert.FromBase64String(encryptionKey), out clientSecret);

                clientId.ShouldBe(clientId);
                clientSecret.ShouldBe(clientSecret);
            }
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
