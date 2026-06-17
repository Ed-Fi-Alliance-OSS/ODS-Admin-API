// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.Jobs;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Services.Jobs;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using EdFi.Ods.AdminApi.InstanceManagement.Provisioners;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Quartz;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Services.Jobs;

[TestFixture]
public class DeleteInstanceJobTests
{
    private sealed class NonDisposingAdminApiDbContext(
        DbContextOptions<AdminApiDbContext> options,
        IConfiguration configuration)
        : AdminApiDbContext(options, configuration)
    {
        public override void Dispose() { }

        public override ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }

    private sealed class NonDisposingSqlServerUsersContext(DbContextOptions options)
        : SqlServerUsersContext(options)
    {
        public override void Dispose() { }

        public override ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }

    private static AdminApiDbContext CreateAdminApiContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AppSettings:DatabaseEngine"] = "SqlServer"
            })
            .Build();

        return new NonDisposingAdminApiDbContext(options, configuration);
    }

    private static SqlServerUsersContext CreateUsersContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new NonDisposingSqlServerUsersContext(options);
    }

    private static IJobExecutionContext CreateJobExecutionContext(int dbInstanceId, string tenantName = null)
    {
        var jobExecutionContext = A.Fake<IJobExecutionContext>();
        var jobDetail = A.Fake<IJobDetail>();
        var jobDataMap = new JobDataMap
        {
            { JobConstants.DbInstanceIdKey, dbInstanceId }
        };

        if (!string.IsNullOrWhiteSpace(tenantName))
        {
            jobDataMap.Put(JobConstants.TenantNameKey, tenantName);
        }

        A.CallTo(() => jobDetail.Key).Returns(new JobKey(JobConstants.DeleteInstanceJobName));
        A.CallTo(() => jobExecutionContext.JobDetail).Returns(jobDetail);
        A.CallTo(() => jobExecutionContext.FireInstanceId).Returns(Guid.NewGuid().ToString());
        A.CallTo(() => jobExecutionContext.MergedJobDataMap).Returns(jobDataMap);

        return jobExecutionContext;
    }

    private static IOptions<AppSettings> CreateOptions(bool multiTenancy = false)
        => Options.Create(new AppSettings
        {
            DatabaseEngine = "SqlServer",
            MultiTenancy = multiTenancy
        });

    private static ITenantConfigurationProvider CreateTenantConfigurationProvider(string tenantIdentifier = null)
    {
        var provider = A.Fake<ITenantConfigurationProvider>();
        var configurations = new Dictionary<string, TenantConfiguration>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(tenantIdentifier))
        {
            configurations[tenantIdentifier] = new TenantConfiguration
            {
                TenantIdentifier = tenantIdentifier,
                AdminConnectionString = "Data Source=(local);Initial Catalog=TenantAdmin;Integrated Security=True;",
                SecurityConnectionString = "Data Source=(local);Initial Catalog=TenantSecurity;Integrated Security=True;"
            };
        }

        A.CallTo(() => provider.Get()).Returns(configurations);

        return provider;
    }

    [Test]
    public void DeleteInstanceJob_ShouldPreventConcurrentExecution()
    {
        typeof(DeleteInstanceJob)
            .GetCustomAttributes(typeof(DisallowConcurrentExecutionAttribute), inherit: true)
            .ShouldNotBeEmpty();
    }

    [Test]
    public async Task Execute_DeletesDatabaseAndOdsInstance_AndSetsDeletedStatus()
    {
        using var adminApiContext = CreateAdminApiContext($"Admin_{Guid.NewGuid()}");
        using var usersContext = CreateUsersContext($"Users_{Guid.NewGuid()}");
        var jobStatusService = A.Fake<IJobStatusService>();
        var tenantConfigurationProvider = CreateTenantConfigurationProvider();
        var tenantConfigurationContextProvider = A.Fake<IContextProvider<TenantConfiguration>>();
        var tenantSpecificDbContextProvider = A.Fake<ITenantSpecificDbContextProvider>();
        var sandboxProvisioner = A.Fake<ISandboxProvisioner>();

        var odsInstance = new OdsInstance { Name = "Sandbox", InstanceType = "Minimal", ConnectionString = "Server=localhost;Database=EdFi_Ods;" };
        usersContext.OdsInstances.Add(odsInstance);
        usersContext.SaveChanges();

        var dbInstance = new Common.Infrastructure.Models.DbInstance
        {
            Name = "Sandbox",
            DatabaseTemplate = "Minimal",
            Status = DbInstanceStatus.PendingDelete.ToString(),
            DatabaseName = "EdFi_Ods_Sandbox_Minimal",
            OdsInstanceId = odsInstance.OdsInstanceId,
            LastRefreshed = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        adminApiContext.DbInstances.Add(dbInstance);
        adminApiContext.SaveChanges();

        var job = new DeleteInstanceJob(
            A.Fake<ILogger<DeleteInstanceJob>>(),
            jobStatusService,
            adminApiContext,
            usersContext,
            tenantConfigurationProvider,
            tenantConfigurationContextProvider,
            tenantSpecificDbContextProvider,
            sandboxProvisioner,
            CreateOptions());

        await job.Execute(CreateJobExecutionContext(dbInstance.Id));

        adminApiContext.DbInstances.Single().Status.ShouldBe(DbInstanceStatus.Deleted.ToString());
        usersContext.OdsInstances.ShouldBeEmpty();
        A.CallTo(() => sandboxProvisioner.DeleteSandboxesAsync("EdFi_Ods_Sandbox_Minimal"))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Execute_SkipsDatabaseDrop_WhenDatabaseNameIsNull()
    {
        using var adminApiContext = CreateAdminApiContext($"Admin_{Guid.NewGuid()}");
        using var usersContext = CreateUsersContext($"Users_{Guid.NewGuid()}");
        var jobStatusService = A.Fake<IJobStatusService>();
        var sandboxProvisioner = A.Fake<ISandboxProvisioner>();

        var dbInstance = new Common.Infrastructure.Models.DbInstance
        {
            Name = "Sandbox",
            DatabaseTemplate = "Minimal",
            Status = DbInstanceStatus.PendingDelete.ToString(),
            DatabaseName = null,
            LastRefreshed = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        adminApiContext.DbInstances.Add(dbInstance);
        adminApiContext.SaveChanges();

        var job = new DeleteInstanceJob(
            A.Fake<ILogger<DeleteInstanceJob>>(),
            jobStatusService,
            adminApiContext,
            usersContext,
            CreateTenantConfigurationProvider(),
            A.Fake<IContextProvider<TenantConfiguration>>(),
            A.Fake<ITenantSpecificDbContextProvider>(),
            sandboxProvisioner,
            CreateOptions());

        await job.Execute(CreateJobExecutionContext(dbInstance.Id));

        adminApiContext.DbInstances.Single().Status.ShouldBe(DbInstanceStatus.Deleted.ToString());
        A.CallTo(() => sandboxProvisioner.DeleteSandboxesAsync(A<string[]>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Execute_SkipsOdsInstanceRemoval_WhenOdsInstanceIdIsNull()
    {
        using var adminApiContext = CreateAdminApiContext($"Admin_{Guid.NewGuid()}");
        using var usersContext = CreateUsersContext($"Users_{Guid.NewGuid()}");
        var jobStatusService = A.Fake<IJobStatusService>();
        var sandboxProvisioner = A.Fake<ISandboxProvisioner>();

        var dbInstance = new Common.Infrastructure.Models.DbInstance
        {
            Name = "Sandbox",
            DatabaseTemplate = "Minimal",
            Status = DbInstanceStatus.PendingDelete.ToString(),
            DatabaseName = "EdFi_Ods_Sandbox_Minimal",
            OdsInstanceId = null,
            LastRefreshed = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        adminApiContext.DbInstances.Add(dbInstance);
        adminApiContext.SaveChanges();

        var job = new DeleteInstanceJob(
            A.Fake<ILogger<DeleteInstanceJob>>(),
            jobStatusService,
            adminApiContext,
            usersContext,
            CreateTenantConfigurationProvider(),
            A.Fake<IContextProvider<TenantConfiguration>>(),
            A.Fake<ITenantSpecificDbContextProvider>(),
            sandboxProvisioner,
            CreateOptions());

        await job.Execute(CreateJobExecutionContext(dbInstance.Id));

        adminApiContext.DbInstances.Single().Status.ShouldBe(DbInstanceStatus.Deleted.ToString());
        usersContext.OdsInstances.ShouldBeEmpty();
    }

    [Test]
    public async Task Execute_DoesNothing_WhenDbInstanceIsNotPendingDelete()
    {
        using var adminApiContext = CreateAdminApiContext($"Admin_{Guid.NewGuid()}");
        using var usersContext = CreateUsersContext($"Users_{Guid.NewGuid()}");
        var jobStatusService = A.Fake<IJobStatusService>();
        var sandboxProvisioner = A.Fake<ISandboxProvisioner>();

        var dbInstance = new Common.Infrastructure.Models.DbInstance
        {
            Name = "Sandbox",
            DatabaseTemplate = "Minimal",
            Status = DbInstanceStatus.Created.ToString(),
            DatabaseName = "EdFi_Ods_Sandbox_Minimal",
            LastRefreshed = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        adminApiContext.DbInstances.Add(dbInstance);
        adminApiContext.SaveChanges();

        var job = new DeleteInstanceJob(
            A.Fake<ILogger<DeleteInstanceJob>>(),
            jobStatusService,
            adminApiContext,
            usersContext,
            CreateTenantConfigurationProvider(),
            A.Fake<IContextProvider<TenantConfiguration>>(),
            A.Fake<ITenantSpecificDbContextProvider>(),
            sandboxProvisioner,
            CreateOptions());

        await job.Execute(CreateJobExecutionContext(dbInstance.Id));

        adminApiContext.DbInstances.Single().Status.ShouldBe(DbInstanceStatus.Created.ToString());
        A.CallTo(() => sandboxProvisioner.DeleteSandboxesAsync(A<string[]>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Execute_SetsDeleteFailed_WhenProvisionerThrows()
    {
        using var adminApiContext = CreateAdminApiContext($"Admin_{Guid.NewGuid()}");
        using var usersContext = CreateUsersContext($"Users_{Guid.NewGuid()}");
        var jobStatusService = A.Fake<IJobStatusService>();
        var sandboxProvisioner = A.Fake<ISandboxProvisioner>();

        A.CallTo(() => sandboxProvisioner.DeleteSandboxesAsync(A<string[]>._))
            .Throws(new InvalidOperationException("Drop failed."));

        var dbInstance = new Common.Infrastructure.Models.DbInstance
        {
            Name = "Sandbox",
            DatabaseTemplate = "Minimal",
            Status = DbInstanceStatus.PendingDelete.ToString(),
            DatabaseName = "EdFi_Ods_Sandbox_Minimal",
            LastRefreshed = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        adminApiContext.DbInstances.Add(dbInstance);
        adminApiContext.SaveChanges();

        var job = new DeleteInstanceJob(
            A.Fake<ILogger<DeleteInstanceJob>>(),
            jobStatusService,
            adminApiContext,
            usersContext,
            CreateTenantConfigurationProvider(),
            A.Fake<IContextProvider<TenantConfiguration>>(),
            A.Fake<ITenantSpecificDbContextProvider>(),
            sandboxProvisioner,
            CreateOptions());

        await job.Execute(CreateJobExecutionContext(dbInstance.Id));

        adminApiContext.DbInstances.Single().Status.ShouldBe(DbInstanceStatus.DeleteFailed.ToString());
    }

    [Test]
    public async Task Execute_UsesTenantSpecificContext_WhenMultiTenancyIsEnabled()
    {
        using var defaultAdminApiContext = CreateAdminApiContext($"Admin_Default_{Guid.NewGuid()}");
        using var tenantAdminApiContext = CreateAdminApiContext($"Admin_Tenant_{Guid.NewGuid()}");
        using var tenantUsersContext = CreateUsersContext($"Users_Tenant_{Guid.NewGuid()}");
        var tenantSpecificDbContextProvider = A.Fake<ITenantSpecificDbContextProvider>();
        var jobStatusService = A.Fake<IJobStatusService>();
        var tenantConfigurationProvider = CreateTenantConfigurationProvider("tenant1");
        var tenantConfigurationContextProvider = A.Fake<IContextProvider<TenantConfiguration>>();
        var sandboxProvisioner = A.Fake<ISandboxProvisioner>();

        A.CallTo(() => tenantSpecificDbContextProvider.GetAdminApiDbContext("tenant1"))
            .Returns(tenantAdminApiContext);
        A.CallTo(() => tenantSpecificDbContextProvider.GetUsersContext("tenant1"))
            .Returns(tenantUsersContext);

        var dbInstance = new Common.Infrastructure.Models.DbInstance
        {
            Name = "Sandbox",
            DatabaseTemplate = "Minimal",
            Status = DbInstanceStatus.PendingDelete.ToString(),
            DatabaseName = "EdFi_Ods_Sandbox_Minimal",
            LastRefreshed = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        tenantAdminApiContext.DbInstances.Add(dbInstance);
        tenantAdminApiContext.SaveChanges();

        var job = new DeleteInstanceJob(
            A.Fake<ILogger<DeleteInstanceJob>>(),
            jobStatusService,
            defaultAdminApiContext,
            A.Fake<IUsersContext>(),
            tenantConfigurationProvider,
            tenantConfigurationContextProvider,
            tenantSpecificDbContextProvider,
            sandboxProvisioner,
            CreateOptions(multiTenancy: true));

        await job.Execute(CreateJobExecutionContext(dbInstance.Id, "tenant1"));

        tenantAdminApiContext.DbInstances.Single().Status.ShouldBe(DbInstanceStatus.Deleted.ToString());
        defaultAdminApiContext.DbInstances.ShouldBeEmpty();
    }
}
