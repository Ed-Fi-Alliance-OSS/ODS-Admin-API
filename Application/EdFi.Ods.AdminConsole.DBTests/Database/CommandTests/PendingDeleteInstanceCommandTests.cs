// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts.Admin.MsSql;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Models;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using FluentValidation;
using NUnit.Framework;
using Shouldly;
using static EdFi.Ods.AdminApi.Features.Applications.AddApplication;
using static EdFi.Ods.AdminApi.Features.Vendors.AddVendor;

namespace EdFi.Ods.AdminConsole.DBTests.Database.CommandTests;

[TestFixture]
public class PendingDeleteInstanceCommandTests : PlatformUsersContextTestBase
{
    [Test]
    public async Task ShouldSetDeletePendingStatusInInstance()
    {
        var newInstanceId = 0;
        var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());

        var repository = new CommandRepository<Instance>(dbContext);
        var addCommand = new AddInstanceCommand(repository);
        var result = await addCommand.Execute(new TestInstance
        {
            TenantId = 1,
            OdsInstanceId = 1,
            Name = "Test Complete Instance",
            InstanceType = "Standard",
            Status = InstanceStatus.Completed.ToString(),
        });

        newInstanceId = result.Id;

        repository = new CommandRepository<Instance>(dbContext);
        var qRepository = new QueriesRepository<Instance>(dbContext);

        var command = new PendingDeleteInstanceCommand(qRepository, repository);
        await command.Execute(newInstanceId);

        var pendingDeleteResult = dbContext.Instances.FirstOrDefault(p => p.Id == newInstanceId);
        pendingDeleteResult.ShouldNotBeNull();
        pendingDeleteResult.Status.ShouldBe(InstanceStatus.Pending_Delete);
    }

    [Test]
    public async Task ShouldNotSetPendingDeleteInstance_NotFoundException()
    {
        AdminConsoleSqlServerUsersContext userDbContext = new(GetUserDbContextOptions());

        var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());

        var repository = new CommandRepository<Instance>(dbContext);
        var qRepository = new QueriesRepository<Instance>(dbContext);

        var command = new PendingDeleteInstanceCommand(qRepository, repository);
        try
        {
            await command.Execute(int.MaxValue);
        }
        catch (Exception ex)
        {
            ex.GetType().ShouldBeEquivalentTo(typeof(NotFoundException<int>));
        }
    }

    [Test]
    public async Task ShouldNotSetPendingDeleteInstance_WhenStatusIsNotCompleted()
    {
        var newInstanceId = 0;
        var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());

        var repository = new CommandRepository<Instance>(dbContext);
        var addCommand = new AddInstanceCommand(repository);
        var result = await addCommand.Execute(new TestInstance
        {
            TenantId = 1,
            OdsInstanceId = 1,
            Name = "Test Complete Instance",
            InstanceType = "Standard",
            Status = InstanceStatus.Error.ToString(),
        });

        newInstanceId = result.Id;

        repository = new CommandRepository<Instance>(dbContext);
        var qRepository = new QueriesRepository<Instance>(dbContext);

        var command = new PendingDeleteInstanceCommand(qRepository, repository);
        try
        {
            await command.Execute(newInstanceId);
        }
        catch (Exception ex)
        {
            ex.GetType().ShouldBeEquivalentTo(typeof(ValidationException));
            ex.Message.ShouldContain(AdminConsoleValidationConstants.OdsIntanceIdStatusIsNotCompleted);
        }
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

        public string Status { get; set; }

        [JsonIgnore]
        public int Id { get; set; }
    }
}
