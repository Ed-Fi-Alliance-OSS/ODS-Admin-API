// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Queries;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminConsole.DBTests.Database.CommandTests;

[TestFixture]
public class GetInstanceByIdQueryTests : PlatformUsersContextTestBase
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
        AddInstanceResult result = null;

        var newInstance = new TestInstance
        {
            TenantId = 1,
            TenantName = "Test Tenant",
            OdsInstanceId = 1,
            Name = "Test Instance",
            InstanceType = "Standard",
            OdsInstanceContexts = new List<OdsInstanceContextModel>(),
            OdsInstanceDerivatives = new List<OdsInstanceDerivativeModel>()
        };

        await TransactionAsync(async dbContext =>
        {
            var repository = new CommandRepository<Instance>(dbContext);
            var command = new AddInstanceCommand(repository);

            result = await command.Execute(newInstance);

            var queryRepository = new QueriesRepository<Instance>(dbContext);
            var query = new GetInstanceByIdQuery(queryRepository);
            var instance = await query.Execute(result.Id);

            instance.Id.ShouldBe(result.Id);
            instance.TenantId.ShouldBe(newInstance.TenantId);
            instance.TenantName.ShouldBe(newInstance.TenantName);
            instance.OdsInstanceId.ShouldBe(newInstance.OdsInstanceId);
            instance.InstanceName.ShouldBe(newInstance.Name);
            instance.InstanceType.ShouldBe(newInstance.InstanceType);
        });
    }

    private class TestInstance : IInstanceRequestModel
    {
        public int OdsInstanceId { get; set; }
        public int TenantId { get; set; }
        public string TenantName { get; set; }
        public string Name { get; set; }

        public string InstanceType { get; set; }

        public ICollection<OdsInstanceContextModel> OdsInstanceContexts { get; set; }

        public ICollection<OdsInstanceDerivativeModel> OdsInstanceDerivatives { get; set; }

        [JsonIgnore]
        public byte[] Credentials { get; set; }

        [JsonIgnore]
        public string Status { get; set; }

        [JsonIgnore]
        public int Id { get; set; }
    }
}
