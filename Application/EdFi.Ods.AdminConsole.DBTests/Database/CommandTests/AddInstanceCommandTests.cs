// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminConsole.DBTests.Database.CommandTests;

[TestFixture]
public class AddInstanceCommandTests : PlatformUsersContextTestBase
{
    [OneTimeSetUp]
    public async Task FixtureSetup()
    {
        await Task.Yield(); // Placeholder if actual setup is needed
    }

    [Test]
    public async Task ShouldExecute()
    {
        await TransactionAsync(async dbContext =>
        {
            var guid = Guid.NewGuid();
            var repository = new CommandRepository<Instance>(dbContext);
            var newInstance = new TestInstance
            {
                TenantId = 1,
                TenantName = "Test Tenant",
                OdsInstanceId = 1,
                Name = "Test Instance " + guid.ToString(),
                InstanceType = "Standard",
                OdsInstanceContexts = new List<OdsInstanceContextModel>(),
                OdsInstanceDerivatives = new List<OdsInstanceDerivativeModel>()
            };

            var command = new AddInstanceCommand(repository);

            var result = await command.Execute(newInstance);

            result.ShouldNotBeNull(); // Ensure a result is returned
            result.Id.ShouldBeGreaterThan(0);
            var persistedInstances = await dbContext.Instances.ToListAsync();
            persistedInstances.Count.ShouldBeGreaterThanOrEqualTo(1);

            var persistedInstance = persistedInstances.FirstOrDefault(p => p.Id == result.Id);
            persistedInstance.ShouldNotBeNull();
            persistedInstance.TenantId.ShouldBe(1);
            persistedInstance.TenantName.ShouldBe("Test Tenant");
            persistedInstance.OdsInstanceId.ShouldBe(1);
            persistedInstance.InstanceName.ShouldBe("Test Instance " + guid.ToString());
            persistedInstance.InstanceType.ShouldBe("Standard");
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
