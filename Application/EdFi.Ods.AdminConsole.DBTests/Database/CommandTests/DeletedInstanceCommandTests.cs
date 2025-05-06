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
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts.Admin.MsSql;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Tenants;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminConsole.DBTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NUnit.Framework;
using Shouldly;
using static EdFi.Ods.AdminApi.Features.Applications.AddApplication;
using static EdFi.Ods.AdminApi.Features.Vendors.AddVendor;

namespace EdFi.Ods.AdminConsole.DBTests.Database.CommandTests
{
    [TestFixture]
    public class DeletedInstanceCommandTests : PlatformUsersContextTestBase
    {
        [Test]
        public async Task ShouldDeleteAnInstance()
        {
            var newInstanceId = 0;
            var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());
            var usersDbContext = new AdminConsoleSqlServerUsersContext(GetUserDbContextOptions());
            string odsIntanceName = "Test Instance to delete " + Guid.NewGuid().ToString();
            var repository = new CommandRepository<Instance>(dbContext);
            var qRepository = new QueriesRepository<Instance>(dbContext);
            var addCommand = new AddInstanceCommand(repository);

            var result = await addCommand.Execute(new TestInstance
            {
                TenantId = 1,
                OdsInstanceId = 1,
                Name = odsIntanceName,
                InstanceType = "Standard",
                Status = InstanceStatus.Pending.ToString(),
            });

            newInstanceId = result.Id;

            repository = new CommandRepository<Instance>(dbContext);


            var tenantService = new TenantService(Testing.GetOptionsSnapshot(), new MemoryCache(new MemoryCacheOptions()));
            var command = new CompleteInstanceCommand(Testing.GetAppSettings(), Testing.GetAdminConsoleSettings(), Testing.GetTestingSettings(), usersDbContext, qRepository, repository, new TenantConfigurationProviderTest(), tenantService);

            var completeResult = await command.Execute(newInstanceId);

            completeResult.ShouldNotBeNull();
            completeResult.Id.ShouldBeGreaterThan(0);
            completeResult.OAuthUrl.ShouldNotBeNull();
            completeResult.ResourceUrl.ShouldNotBeNull();
            usersDbContext.OdsInstances.ToList().Count.ShouldBeGreaterThanOrEqualTo(1);
            var odsInstance = usersDbContext.OdsInstances.FirstOrDefault(p => p.OdsInstanceId == completeResult.OdsInstanceId);
            odsInstance.Name.ShouldBe(odsIntanceName);
            var apiClientOdsInstance = usersDbContext.ApiClientOdsInstances.Where(p => p.OdsInstance.OdsInstanceId == odsInstance.OdsInstanceId);
            apiClientOdsInstance.Count().ShouldBeGreaterThan(0);
            var pendingDeleteCommand = new PendingDeleteInstanceCommand(qRepository, repository);
            await pendingDeleteCommand.Execute(newInstanceId);

            var pendingDeleteResult = dbContext.Instances.FirstOrDefault(p => p.Id == newInstanceId);
            pendingDeleteResult.ShouldNotBeNull();
            pendingDeleteResult.Status.ShouldBe(InstanceStatus.Pending_Delete);

            var deletedCommand = new DeletedInstanceCommand(usersDbContext, qRepository, repository, Testing.GetTestingSettings());
            await deletedCommand.Execute(newInstanceId);

            var deletedResult = dbContext.Instances.FirstOrDefault(p => p.Id == newInstanceId);
            deletedResult.ShouldNotBeNull();
            deletedResult.Status.ShouldBe(InstanceStatus.Deleted);
            var odsInstanceDeleted = usersDbContext.OdsInstances.FirstOrDefault(p => p.OdsInstanceId == completeResult.OdsInstanceId);
            odsInstanceDeleted.ShouldBeNull();
            var apiClientOdsInstanceDeleted = usersDbContext.ApiClientOdsInstances.Where(p => p.OdsInstance.OdsInstanceId == odsInstance.OdsInstanceId);
            apiClientOdsInstanceDeleted.Count().ShouldBe(0);

        }

        [Test]
        public async Task ShouldNotDelete_NotFoundException()
        {
            var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());
            var dbUsersContext = new AdminConsoleSqlServerUsersContext(GetUserDbContextOptions());

            var repository = new CommandRepository<Instance>(dbContext);
            var qRepository = new QueriesRepository<Instance>(dbContext);

            var command = new DeletedInstanceCommand(dbUsersContext, qRepository, repository, Testing.GetTestingSettings());
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
        public async Task ShouldNotDeleteInstance_WhenAnExceptionIsThrownInTransaction()
        {

            var newInstanceId = 0;
            var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());
            var usersDbContext = new AdminConsoleSqlServerUsersContext(GetUserDbContextOptions());

            var repository = new CommandRepository<Instance>(dbContext);
            var qRepository = new QueriesRepository<Instance>(dbContext);
            var addCommand = new AddInstanceCommand(repository);

            var result = await addCommand.Execute(new TestInstance
            {
                TenantId = 1,
                OdsInstanceId = 1,
                Name = "Test Instance to delete " + Guid.NewGuid().ToString(),
                InstanceType = "Standard",
                Status = InstanceStatus.Pending.ToString(),
            });

            newInstanceId = result.Id;

            repository = new CommandRepository<Instance>(dbContext);


            var tenantService = new TenantService(Testing.GetOptionsSnapshot(), new MemoryCache(new MemoryCacheOptions()));
            var command = new CompleteInstanceCommand(Testing.GetAppSettings(), Testing.GetAdminConsoleSettings(), Testing.GetTestingSettings(), usersDbContext, qRepository, repository, new TenantConfigurationProviderTest(), tenantService);

            var completeResult = await command.Execute(newInstanceId);

            completeResult.ShouldNotBeNull();
            completeResult.Id.ShouldBeGreaterThan(0);
            completeResult.OAuthUrl.ShouldNotBeNull();
            completeResult.ResourceUrl.ShouldNotBeNull();
            usersDbContext.OdsInstances.ToList().Count.ShouldBeGreaterThanOrEqualTo(1);
            var odsInstance = usersDbContext.OdsInstances.FirstOrDefault(p => p.OdsInstanceId == completeResult.OdsInstanceId);

            var pendingDeleteCommand = new PendingDeleteInstanceCommand(qRepository, repository);
            await pendingDeleteCommand.Execute(newInstanceId);

            var pendingDeleteResult = dbContext.Instances.FirstOrDefault(p => p.Id == newInstanceId);
            pendingDeleteResult.ShouldNotBeNull();
            pendingDeleteResult.Status.ShouldBe(InstanceStatus.Pending_Delete);

            var deletedCommand = new DeletedInstanceCommand(usersDbContext, qRepository, repository, Testing.GetTestingSettings(injectException: true));
            try
            {
                await deletedCommand.Execute(newInstanceId);
            }
            catch (Exception ex)
            {
                ex.GetType().ShouldBeEquivalentTo(typeof(AdminApiException));
                ex.Message.ShouldBe("Exception to test");
                //check for the data
                var deletedResult = dbContext.Instances.FirstOrDefault(p => p.Id == newInstanceId);
                deletedResult.ShouldNotBeNull();
                deletedResult.Status.ShouldBe(InstanceStatus.Delete_Failed);
            }
        }
    }
}
