// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts.Admin.MsSql;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using NUnit.Framework;
using Shouldly;
using static EdFi.Ods.AdminApi.Features.Applications.AddApplication;
using static EdFi.Ods.AdminApi.Features.Vendors.AddVendor;

namespace EdFi.Ods.AdminConsole.DBTests.Database.CommandTests;

[TestFixture]
public class CompleteInstanceCommandTests : PlatformUsersContextTestBase
{
    [Test]
    public async Task ShouldCompleteInstance()
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
                Name = "Test Complete Instance",
                InstanceType = "Standard"
            });

            newInstanceId = result.Id;
        });

        var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());

        var repository = new CommandRepository<Instance>(dbContext);
        var qRepository = new QueriesRepository<Instance>(dbContext);

        var command = new CompleteInstanceCommand(Testing.GetAdminConsoleSettings(), userDbContext, qRepository, repository);
        var completeResult = await command.Execute(newInstanceId);

        completeResult.ShouldNotBeNull();
        completeResult.Id.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task ShouldNotCompleteInstance_NotFoundException()
    {
        AdminConsoleSqlServerUsersContext userDbContext = new(GetUserDbContextOptions());

        var dbContext = new AdminConsoleMsSqlContext(GetDbContextOptions());

        var repository = new CommandRepository<Instance>(dbContext);
        var qRepository = new QueriesRepository<Instance>(dbContext);
        Instance completeResult = null;

        var command = new CompleteInstanceCommand(Testing.GetAdminConsoleSettings(), userDbContext, qRepository, repository);
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

    private class TestInstance : IInstanceRequestModel
    {
        public int OdsInstanceId { get; set; }
        public int TenantId { get; set; }
        public string? TenantName { get; set; }
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
