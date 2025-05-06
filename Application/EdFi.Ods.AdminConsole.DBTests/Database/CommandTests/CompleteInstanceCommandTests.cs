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
using NUnit.Framework;
using Shouldly;
using static EdFi.Ods.AdminApi.Features.Applications.AddApplication;
using static EdFi.Ods.AdminApi.Features.Vendors.AddVendor;

namespace EdFi.Ods.AdminConsole.DBTests.Database.CommandTests;

[TestFixture]
public class CompleteInstanceCommandTests : PlatformUsersContextTestBase
{
    [Test]
    public async Task ShouldCompleteInstanceAsync()
    {
        AdminConsoleSqlServerUsersContext userDbContext = new(GetUserDbContextOptions());
        var addVendorCommand = new AddVendorCommand(userDbContext);
        var addApplicationCommand = new AddApplicationCommand(userDbContext);
        var guid = Guid.NewGuid();
        string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=admin;Database=\"Ods_Test Complete Instance " + guid.ToString() + "\";Pooling=False";
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
                Name = "Test Complete Instance " + guid.ToString(),
                InstanceType = "Standard",
                TenantName = "tenant1"
            });

            newInstanceId = result.Id;
        });

        var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());

        var repository = new CommandRepository<Instance>(dbContext);
        var qRepository = new QueriesRepository<Instance>(dbContext);
        var tenantService = new TenantService(Testing.GetOptionsSnapshot(), new MemoryCache(new MemoryCacheOptions()));

        var command = new CompleteInstanceCommand(Testing.GetAppSettings(), Testing.GetAdminConsoleSettings(), Testing.GetTestingSettings(), userDbContext, qRepository, repository, new TenantConfigurationProviderTest(), tenantService);
        var completeResult = await command.Execute(newInstanceId);

        completeResult.ShouldNotBeNull();
        completeResult.Id.ShouldBeGreaterThan(0);
        completeResult.OAuthUrl.ShouldNotBeNull();
        completeResult.ResourceUrl.ShouldNotBeNull();

        userDbContext.OdsInstances.ToList().Count.ShouldBeGreaterThanOrEqualTo(1);

        var odsInstance = userDbContext.OdsInstances.FirstOrDefault(p => p.OdsInstanceId == completeResult.OdsInstanceId);
        odsInstance.ShouldNotBeNull();
        odsInstance.Name.ShouldBe("Test Complete Instance " + guid.ToString());
        odsInstance.InstanceType.ShouldBe("Standard");
        odsInstance.ConnectionString.ShouldBe(connectionString);
    }

    [Test]
    public async Task ShouldNotCompleteInstance_NotFoundException()
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
                Name = "Test Complete Instance " + guid.ToString(),
                InstanceType = "Standard"
            });

            newInstanceId = result.Id;
        });

        var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());

        var repository = new CommandRepository<Instance>(dbContext);
        var qRepository = new QueriesRepository<Instance>(dbContext);
        Instance completeResult = null;
        var tenantService = new TenantService(Testing.GetOptionsSnapshot(), new MemoryCache(new MemoryCacheOptions()));
        var command = new CompleteInstanceCommand(Testing.GetAppSettings(), Testing.GetAdminConsoleSettings(), Testing.GetTestingSettings(), userDbContext, qRepository, repository, new TenantConfigurationProviderTest(), tenantService);
        try
        {
            completeResult = await command.Execute(int.MaxValue);
        }
        catch (Exception ex)
        {
            ex.GetType().ShouldBeEquivalentTo(typeof(NotFoundException<int>));
        }

        completeResult.ShouldBeNull();
    }

    [Test]
    public async Task ShouldNotCompleteInstance_WhenAnExceptionIsThrownInTransaction()
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
                Name = "Test Complete Instance " + guid.ToString(),
                InstanceType = "Standard"
            });

            newInstanceId = result.Id;
        });

        var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());

        var repository = new CommandRepository<Instance>(dbContext);
        var qRepository = new QueriesRepository<Instance>(dbContext);
        Instance completeResult = null;
        var tenantService = new TenantService(Testing.GetOptionsSnapshot(), new MemoryCache(new MemoryCacheOptions()));
        var command = new CompleteInstanceCommand(Testing.GetAppSettings(), Testing.GetAdminConsoleSettings(), Testing.GetTestingSettings(injectException: true), userDbContext, qRepository, repository, new TenantConfigurationProviderTest(), tenantService);
        try
        {
            completeResult = await command.Execute(newInstanceId);
        }
        catch (Exception ex)
        {
            ex.GetType().ShouldBeEquivalentTo(typeof(AdminApiException));
            ex.Message.ShouldBe("Exception to test");
            //check for the data
            var data = await qRepository.GetAllAsync();
            var dataResult = data.FirstOrDefault(p => p.Id == newInstanceId);
            dataResult.ShouldNotBeNull();
            dataResult.Id.ShouldBe(newInstanceId);
            dataResult.Status.ShouldNotBe(InstanceStatus.Completed);
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

        [JsonIgnore]
        public string Status { get; set; }

        [JsonIgnore]
        public int Id { get; set; }
    }
}
