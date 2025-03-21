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
using EdFi.Ods.AdminConsole.DBTests.Common;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminConsole.DBTests.Database.CommandTests
{
    [TestFixture]
    public class RenameFailedInstanceCommandTests : PlatformUsersContextTestBase
    {
        [Test]
        public async Task ShouldSetRenameFailedStatusInInstance()
        {
            var newInstanceId = 0;
            var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());

            var repository = new CommandRepository<Instance>(dbContext);
            var addCommand = new AddInstanceCommand(repository);
            var result = await addCommand.Execute(new TestInstance
            {
                TenantId = 1,
                OdsInstanceId = 1,
                Name = "Test Pending Rename Instance" + Guid.NewGuid().ToString(),
                InstanceType = "Standard",
                Status = InstanceStatus.Pending_Rename.ToString(),
            });

            newInstanceId = result.Id;

            repository = new CommandRepository<Instance>(dbContext);
            var qRepository = new QueriesRepository<Instance>(dbContext);

            var renameFailedCommand = new RenameFailedInstanceCommand(qRepository, repository);
            await renameFailedCommand.Execute(newInstanceId);
            var renameFailedResult = dbContext.Instances.FirstOrDefault(p => p.Id == newInstanceId);
            renameFailedResult.ShouldNotBeNull();
            renameFailedResult.Status.ShouldBe(InstanceStatus.Rename_Failed);
        }

        [Test]
        public async Task ShouldNotSetRenameFailed_NotFoundException()
        {
            AdminConsoleSqlServerUsersContext userDbContext = new(GetUserDbContextOptions());

            var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());

            var repository = new CommandRepository<Instance>(dbContext);
            var qRepository = new QueriesRepository<Instance>(dbContext);

            var command = new RenameFailedInstanceCommand(qRepository, repository);
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
        public async Task ShouldNotSetDeleteFailed_WhenStatusIsNotPendingRename()
        {
            var newInstanceId = 0;
            var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());

            var repository = new CommandRepository<Instance>(dbContext);
            var addCommand = new AddInstanceCommand(repository);
            var result = await addCommand.Execute(new TestInstance
            {
                TenantId = 1,
                OdsInstanceId = 1,
                Name = "Test Error Instance" + Guid.NewGuid().ToString(),
                InstanceType = "Standard",
                Status = InstanceStatus.Error.ToString(),
            });

            newInstanceId = result.Id;

            repository = new CommandRepository<Instance>(dbContext);
            var qRepository = new QueriesRepository<Instance>(dbContext);

            var command = new RenameFailedInstanceCommand(qRepository, repository);
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
