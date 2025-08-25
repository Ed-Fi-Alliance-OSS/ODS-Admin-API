// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts.Admin.MsSql;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminConsole.DBTests.Common;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminConsole.DBTests.Database.CommandTests
{
    [TestFixture]
    public class DeleteInstanceFailedCommandTests : PlatformUsersContextTestBase
    {
        [Test]
        public async Task ShouldSetDeleteFailedStatusInInstance()
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

            var pendingDeleteCommand = new PendingDeleteInstanceCommand(qRepository, repository);
            await pendingDeleteCommand.Execute(newInstanceId);

            var pendingDeleteResult = dbContext.Instances.FirstOrDefault(p => p.Id == newInstanceId);
            pendingDeleteResult.ShouldNotBeNull();
            pendingDeleteResult.Status.ShouldBe(InstanceStatus.Pending_Delete);

            var deleteFailedCommand = new DeleteInstanceFailedCommand(qRepository, repository);
            await deleteFailedCommand.Execute(newInstanceId);
            var deleteFailedResult = dbContext.Instances.FirstOrDefault(p => p.Id == newInstanceId);
            pendingDeleteResult.ShouldNotBeNull();
            pendingDeleteResult.Status.ShouldBe(InstanceStatus.Delete_Failed);
        }

        [Test]
        public async Task ShouldNotSetDeleteFailed_NotFoundException()
        {
            AdminConsoleSqlServerUsersContext userDbContext = new(GetUserDbContextOptions());

            var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());

            var repository = new CommandRepository<Instance>(dbContext);
            var qRepository = new QueriesRepository<Instance>(dbContext);

            var command = new DeleteInstanceFailedCommand(qRepository, repository);
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
        public async Task ShouldNotSetDeleteFailed_WhenStatusIsNotPendingDelete()
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

            var command = new DeleteInstanceFailedCommand(qRepository, repository);
            try
            {
                await command.Execute(newInstanceId);
            }
            catch (AdminApiException ex)
            {
                ex.StatusCode.ShouldBe(HttpStatusCode.Conflict);
            }
        }
    }
}
