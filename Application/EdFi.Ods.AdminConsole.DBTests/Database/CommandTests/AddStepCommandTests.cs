// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public async Task ShouldExecuteAsync()
    {
        var stepDocument = "[{\"number\":1,\"description\":\"Step1\",\"startedAt\":\"2022-01-01T09:00:00\",\"completedAt\":\"2022-01-01T09:30:00\",\"status\":\"Completed\"},{\"number\":2,\"description\":\"Step2\",\"startedAt\":\"2022-01-01T09:30:00\",\"completedAt\":\"2022-01-01T09:45:00\",\"status\":\"Completed\"},{\"number\":3,\"description\":\"Step3\",\"startedAt\":\"2022-01-01T09:45:00\",\"completedAt\":\"2022-01-01T10:00:00\",\"status\":\"Completed\"}]";

        var aes256SymmetricStringEncryptionProvider = new Aes256SymmetricStringEncryptionProvider();
        var encryptionKey = Testing.GetEncryptionKeyResolver().GetEncryptionKey();

        await TransactionAsync(async dbContext =>
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

            var persistedStep = dbContext.Steps;
            persistedStep.Count().ShouldBeGreaterThanOrEqualTo(1);
            persistedStep.First().DocId.GetValueOrDefault().ShouldBeGreaterThan(0);
            persistedStep.First().TenantId.ShouldBe(1);
            persistedStep.First().InstanceId.ShouldBe(1);
            persistedStep.First().EdOrgId.ShouldBe(1);

            JsonNode jnDocument = JsonNode.Parse(persistedStep.First().Document);
            jnDocument.ShouldNotBeNull();
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
