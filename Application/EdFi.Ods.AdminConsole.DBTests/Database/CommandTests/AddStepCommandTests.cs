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
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Steps.Commands;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminConsole.DBTests.Database.CommandTests;

[TestFixture]
public class AddStepCommandTests : PlatformUsersContextTestBase
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
        var stepDocument = "[{\"number\":1,\"description\":\"Step1\",\"startedAt\":\"2022-01-01T09:00:00\",\"completedAt\":\"2022-01-01T09:30:00\",\"status\":\"Completed\"},{\"number\":2,\"description\":\"Step2\",\"startedAt\":\"2022-01-01T09:30:00\",\"completedAt\":\"2022-01-01T09:45:00\",\"status\":\"Completed\"},{\"number\":3,\"description\":\"Step3\",\"startedAt\":\"2022-01-01T09:45:00\",\"completedAt\":\"2022-01-01T10:00:00\",\"status\":\"Completed\"}]";

        var aes256SymmetricStringEncryptionProvider = new Aes256SymmetricStringEncryptionProvider();
        var encryptionKey = Testing.GetEncryptionKeyResolver().GetEncryptionKey();

        Transaction(async dbContext =>
        {
            var repository = new CommandRepository<Step>(dbContext);
            var newStep = new TestStep
            {
                InstanceId = 1,
                TenantId = 1,
                EdOrgId = 1,
                Document = stepDocument
            };

            var command = new AddStepCommand(repository);

            var result = await command.Execute(newStep);
        });

        Transaction(dbContext =>
        {
            var persistedStep = dbContext.Steps;
            persistedStep.Count().ShouldBe(1);
            persistedStep.First().DocId.ShouldBe(1);
            persistedStep.First().TenantId.ShouldBe(1);
            persistedStep.First().InstanceId.ShouldBe(1);
            persistedStep.First().EdOrgId.ShouldBe(1);

            JsonNode jnDocument = JsonNode.Parse(persistedStep.First().Document);

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

    private class TestStep : IAddStepModel
    {
        public int DocId { get; }
        public int TenantId { get; set; }
        public int InstanceId { get; set; }
        public int? EdOrgId { get; set; }
        public string Document { get; set; }
    }
}
