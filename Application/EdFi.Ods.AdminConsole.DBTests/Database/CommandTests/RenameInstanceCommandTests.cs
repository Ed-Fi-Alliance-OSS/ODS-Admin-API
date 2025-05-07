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
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Tenants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;
using Shouldly;
using static EdFi.Ods.AdminApi.Features.Applications.AddApplication;
using static EdFi.Ods.AdminApi.Features.Vendors.AddVendor;

namespace EdFi.Ods.AdminConsole.DBTests.Database.CommandTests;

[TestFixture]
public class RenameInstanceCommandTests : PlatformUsersContextTestBase
{
    [Test]
    public async Task ShouldRenameInstance()
    {
        AdminConsoleSqlServerUsersContext userDbContext = new(GetUserDbContextOptions());
        var addVendorCommand = new AddVendorCommand(userDbContext);
        var addApplicationCommand = new AddApplicationCommand(userDbContext);
        var guid = Guid.NewGuid();
        var vendor = addVendorCommand.Execute(new AddVendorRequest
        {
            Company = Testing.GetAdminConsoleSettings().Value.VendorCompany,
            NamespacePrefixes = "joe@test.com",
            ContactName = Testing.GetAdminConsoleSettings().Value.VendorCompany,
            ContactEmailAddress = "test"
        });

        _ = addApplicationCommand.Execute(new AddApplicationRequest
        {
            ApplicationName = Testing.GetAdminConsoleSettings().Value.ApplicationName,
            ClaimSetName = "test",
            ProfileIds = null,
            VendorId = vendor?.VendorId ?? 0
        }, Testing.GetAppSettings());

        var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());

        var repository = new CommandRepository<Instance>(dbContext);
        var qRepository = new QueriesRepository<Instance>(dbContext);

        var instance = await repository.AddAsync(new Instance()
        {
            TenantId = 1,
            TenantName = "tenant1",
            InstanceName = "Test Rename Instance " + guid.ToString(),
            InstanceType = "Standard",
            Status = InstanceStatus.Pending
        });

        var tenantService = new TenantService(Testing.GetOptionsSnapshot(), new MemoryCache(new MemoryCacheOptions()));
        var command = new CompleteInstanceCommand(Testing.GetAppSettings(), Testing.GetAdminConsoleSettings(), Testing.GetTestingSettings(), userDbContext, qRepository, repository, tenantService);
        var completeResult = await command.Execute(instance.Id, "Host=localhost;Port=5432;Username=postgres;Password=admin;Database=\"Test Rename Instance " + guid.ToString() + "\";Pooling=False");
        completeResult.Status.ShouldBe(InstanceStatus.Completed);

        var commandEdit = new EditInstanceCommand(repository, qRepository, dbContext, userDbContext);

        var editInstanceData = new Mock<IInstanceRequestModel>();
        editInstanceData.Setup(v => v.OdsInstanceId).Returns(completeResult.OdsInstanceId.GetValueOrDefault());
        editInstanceData.Setup(v => v.TenantId).Returns(completeResult.TenantId);
        editInstanceData.Setup(v => v.TenantName).Returns(completeResult.TenantName);
        editInstanceData.Setup(v => v.Name).Returns("Updated Instance Rename " + guid.ToString());
        editInstanceData.Setup(v => v.InstanceType).Returns(completeResult.InstanceType);
        editInstanceData.Setup(v => v.OdsInstanceContexts).Returns([]);
        editInstanceData.Setup(v => v.OdsInstanceDerivatives).Returns([]);

        var resultEdit = await commandEdit.Execute(instance.Id, editInstanceData.Object);

        resultEdit.Status.ShouldBe(InstanceStatus.Pending_Rename);

        var commandRename = new RenameInstanceCommand(Testing.GetAdminConsoleSettings(), userDbContext, qRepository, repository);
        var renameResult = await commandRename.Execute(instance.Id, "Host=localhost;Port=5432;Username=postgres;Password=admin;Database=\"Updated Instance Rename " + guid.ToString() + "\";Pooling=False");

        renameResult.ShouldNotBeNull();
        renameResult.Id.ShouldBeGreaterThan(0);
        renameResult.Status.ShouldBe(InstanceStatus.Completed);

        userDbContext.OdsInstances.First(p => p.OdsInstanceId == renameResult.OdsInstanceId).ShouldNotBeNull();
        userDbContext.OdsInstances.First(p => p.OdsInstanceId == renameResult.OdsInstanceId).Name.ShouldBe("Updated Instance Rename " + guid.ToString());
        userDbContext.OdsInstances.First(p => p.OdsInstanceId == renameResult.OdsInstanceId).InstanceType.ShouldBe("Standard");
        userDbContext.OdsInstances.First(p => p.OdsInstanceId == renameResult.OdsInstanceId).ConnectionString.ShouldBe("Host=localhost;Port=5432;Username=postgres;Password=admin;Database=\"Updated Instance Rename " + guid.ToString() + "\";Pooling=False");
    }

    [Test]
    public async Task ShouldNotRenameInstance_NotFoundException()
    {
        AdminConsoleSqlServerUsersContext userDbContext = new(GetUserDbContextOptions());
        var addVendorCommand = new AddVendorCommand(userDbContext);
        var addApplicationCommand = new AddApplicationCommand(userDbContext);

        var vendor = addVendorCommand.Execute(new AddVendorRequest
        {
            Company = Testing.GetAdminConsoleSettings().Value.VendorCompany,
            NamespacePrefixes = "joe@test.com",
            ContactName = Testing.GetAdminConsoleSettings().Value.VendorCompany,
            ContactEmailAddress = "test"
        });

        var application = addApplicationCommand.Execute(new AddApplicationRequest
        {
            ApplicationName = Testing.GetAdminConsoleSettings().Value.ApplicationName,
            ClaimSetName = "test",
            ProfileIds = null,
            VendorId = vendor?.VendorId ?? 0
        }, Testing.GetAppSettings());

        var newInstanceId = 0;

        await TransactionAsync(async dbContext =>
        {
            var repository = new CommandRepository<Instance>(dbContext);
            var command = new AddInstanceCommand(repository);

            var result = await command.Execute(new TestInstance
            {
                TenantId = 1,
                OdsInstanceId = 1,
                Name = "Test Rename Instance",
                InstanceType = "Standard"
            });

            newInstanceId = result.Id;
        });

        var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());

        var repository = new CommandRepository<Instance>(dbContext);
        var qRepository = new QueriesRepository<Instance>(dbContext);
        Instance renameResult = null;

        var command = new RenameInstanceCommand(Testing.GetAdminConsoleSettings(), userDbContext, qRepository, repository);
        try
        {
            renameResult = await command.Execute(int.MaxValue, "some connection string");
        }
        catch (Exception ex)
        {
            ex.GetType().ShouldBeEquivalentTo(typeof(NotFoundException<int>));
        }

        renameResult.ShouldBeNull();
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
