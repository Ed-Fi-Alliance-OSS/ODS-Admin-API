// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Steps.Commands;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Steps.Queries;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminConsole.DBTests.Database.CommandTests;

[TestFixture]
public class GetStepsByTenantIdQueryTests : PlatformUsersContextTestBase
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
        Step result = null;

        var newStep = new TestStep
        {
            InstanceId = 1,
            TenantId = 1,
            EdOrgId = 1,
            Document = stepDocument
        };

        Transaction(async dbContext =>
        {
            var repository = new CommandRepository<Step>(dbContext);
            var command = new AddStepCommand(repository);

            result = await command.Execute(newStep);
        });

        Transaction(async dbContext =>
        {
            var repository = new QueriesRepository<Step>(dbContext);
            var query = new GetStepsByTenantIdQuery(repository);
            var steps = await query.Execute(result.TenantId);
            steps.Count().ShouldBe(1);
            steps.FirstOrDefault().DocId.ShouldBe(result.DocId);
            steps.FirstOrDefault().TenantId.ShouldBe(newStep.TenantId);
            steps.FirstOrDefault().InstanceId.ShouldBe(newStep.InstanceId);
            steps.FirstOrDefault().EdOrgId.ShouldBe(newStep.EdOrgId);
            steps.FirstOrDefault().Document.ShouldBe(newStep.Document);
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
