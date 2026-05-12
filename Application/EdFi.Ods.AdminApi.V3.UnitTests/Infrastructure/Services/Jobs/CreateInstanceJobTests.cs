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
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features.DbInstances;
using EdFi.Ods.AdminApi.V3.Infrastructure;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Jobs;
using EdFi.Ods.AdminApi.V3.Infrastructure.Services.Tenants;
using EdFi.Ods.AdminApi.InstanceManagement.Provisioners;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Quartz;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Services.Jobs;

[TestFixture]
public class CreateInstanceJobTests
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

    private static AdminApiDbContext CreateAdminApiContext(string databaseName, IConfiguration configuration)
    {
        var options = new DbContextOptionsBuilder<AdminApiDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

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

        A.CallTo(() => jobDetail.Key).Returns(new JobKey(JobConstants.CreateInstanceJobName));
        A.CallTo(() => jobExecutionContext.JobDetail).Returns(jobDetail);
        A.CallTo(() => jobExecutionContext.FireInstanceId).Returns(Guid.NewGuid().ToString());
        A.CallTo(() => jobExecutionContext.MergedJobDataMap).Returns(jobDataMap);

        return jobExecutionContext;
    }

    private static IConfiguration CreateConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:EdFi_Ods"] = "Data Source=(local);Initial Catalog=EdFi_Admin;Integrated Security=True;",
                ["ConnectionStrings:EdFi_Master"] = "Data Source=(local);Initial Catalog=master;Integrated Security=True;",
                ["Tenants:tenant1:ConnectionStrings:EdFi_Ods"] = "Data Source=(local);Initial Catalog=TenantTemplateDb;Integrated Security=True;",
                ["Tenants:tenant1:ConnectionStrings:EdFi_Master"] = "Data Source=(local);Initial Catalog=TenantMaster;Integrated Security=True;"
            })
            .Build();

    private static IOptions<AppSettings> CreateOptions(bool multiTenancy = false)
        => Options.Create(new AppSettings
        {
            DatabaseEngine = "SqlServer",
            EncryptionKey = Convert.ToBase64String(new byte[32]),
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
                SecurityConnectionString = "Data Source=(local);Initial Catalog=TenantSecurity;Integrated Security=True;",
                OdsConnectionString = "Data Source=(local);Initial Catalog=TenantTemplateDb;Integrated Security=True;",
                MasterConnectionString = "Data Source=(local);Initial Catalog=TenantMaster;Integrated Security=True;"
            };
        }

        A.CallTo(() => provider.Get()).Returns(configurations);

        return provider;
    }

    [Test]
    public void CreateInstanceJob_ShouldPreventConcurrentExecution()
    {
        typeof(CreateInstanceJob)
            .GetCustomAttributes(typeof(DisallowConcurrentExecutionAttribute), inherit: true)
            .ShouldNotBeEmpty();
    }

    [TestCase("Sandbox", "Minimal", "EdFi_Ods_Sandbox_Minimal")]
    [TestCase("My District", "Minimal", "EdFi_Ods_My_District_Minimal")]
    [TestCase("EdFi_Ods_Sandbox", "Minimal", "EdFi_Ods_Sandbox_Minimal")]
    [TestCase("EDFI   ODS Sandbox", "Minimal", "EdFi_Ods_Sandbox_Minimal")]
    [TestCase("EdFi Ods", "Minimal", "EdFi_Ods_Minimal")]
    public void BuildDatabaseName_UsesCanonicalFormat(string name, string databaseTemplate, string expectedDatabaseName)
    {
        DbInstanceDatabaseNameFormatter.Build(name, databaseTemplate).ShouldBe(expectedDatabaseName);
    }

    [Test]
    public async Task Execute_CreatesOdsInstance_AndCompletesDbInstance()
    {
        var configuration = CreateConfiguration();
        using var adminApiContext = CreateAdminApiContext($"Admin_{Guid.NewGuid()}", configuration);
        using var usersContext = CreateUsersContext($"Users_{Guid.NewGuid()}");
        var jobStatusService = A.Fake<IJobStatusService>();
        var tenantConfigurationProvider = CreateTenantConfigurationProvider();
        var tenantConfigurationContextProvider = A.Fake<IContextProvider<TenantConfiguration>>();
        var tenantSpecificDbContextProvider = A.Fake<ITenantSpecificDbContextProvider>();
        var encryptionProvider = A.Fake<ISymmetricStringEncryptionProvider>();
        var sandboxProvisioner = A.Fake<ISandboxProvisioner>();
        string plaintextConnectionString = null;

        A.CallTo(() => encryptionProvider.Encrypt(A<string>._, A<byte[]>._))
            .Invokes((string connectionString, byte[] _) => plaintextConnectionString = connectionString)
            .ReturnsLazily((string connectionString, byte[] _) => $"encrypted::{connectionString}");

        var dbInstance = new Common.Infrastructure.Models.DbInstance
        {
            Name = "Sandbox",
            DatabaseTemplate = "Minimal",
            Status = DbInstanceStatus.PendingCreate.ToString(),
            LastRefreshed = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        adminApiContext.DbInstances.Add(dbInstance);
        adminApiContext.SaveChanges();

        var job = new CreateInstanceJob(
            A.Fake<ILogger<CreateInstanceJob>>(),
            jobStatusService,
            adminApiContext,
            usersContext,
            tenantConfigurationProvider,
            tenantConfigurationContextProvider,
            tenantSpecificDbContextProvider,
            encryptionProvider,
            sandboxProvisioner,
            CreateOptions(),
            configuration,
            new DbConnectionStringBuilderAdapterFactory(new SqlConnectionStringBuilderAdapter()));

        await job.Execute(CreateJobExecutionContext(dbInstance.Id));

        var persistedDbInstance = adminApiContext.DbInstances.Single();
        var persistedOdsInstance = usersContext.OdsInstances.Single();
        const string expectedDatabaseName = "EdFi_Ods_Sandbox_Minimal";

        persistedDbInstance.Status.ShouldBe(DbInstanceStatus.Created.ToString());
        persistedDbInstance.DatabaseName.ShouldBe(expectedDatabaseName);
        persistedDbInstance.OdsInstanceId.ShouldNotBeNull();
        persistedDbInstance.OdsInstanceName.ShouldBe("Sandbox");
        persistedOdsInstance.Name.ShouldBe("Sandbox");
        persistedOdsInstance.InstanceType.ShouldBe("Minimal");
        A.CallTo(() => sandboxProvisioner.AddSandboxAsync(expectedDatabaseName, SandboxType.Minimal))
            .MustHaveHappenedOnceExactly();
        plaintextConnectionString.ShouldNotBeNull();
        plaintextConnectionString.ShouldContain($"Initial Catalog={expectedDatabaseName}");
        persistedOdsInstance.ConnectionString.ShouldContain(expectedDatabaseName);
    }

    [Test]
    public async Task Execute_FormatsDatabaseName_WhenNameContainsSpaces()
    {
        var configuration = CreateConfiguration();
        using var adminApiContext = CreateAdminApiContext($"Admin_{Guid.NewGuid()}", configuration);
        using var usersContext = CreateUsersContext($"Users_{Guid.NewGuid()}");
        var jobStatusService = A.Fake<IJobStatusService>();
        var tenantConfigurationProvider = CreateTenantConfigurationProvider();
        var tenantConfigurationContextProvider = A.Fake<IContextProvider<TenantConfiguration>>();
        var tenantSpecificDbContextProvider = A.Fake<ITenantSpecificDbContextProvider>();
        var encryptionProvider = A.Fake<ISymmetricStringEncryptionProvider>();
        var sandboxProvisioner = A.Fake<ISandboxProvisioner>();

        var dbInstance = new Common.Infrastructure.Models.DbInstance
        {
            Name = "My District",
            DatabaseTemplate = "Minimal",
            Status = DbInstanceStatus.PendingCreate.ToString(),
            LastRefreshed = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        adminApiContext.DbInstances.Add(dbInstance);
        adminApiContext.SaveChanges();

        var job = new CreateInstanceJob(
            A.Fake<ILogger<CreateInstanceJob>>(),
            jobStatusService,
            adminApiContext,
            usersContext,
            tenantConfigurationProvider,
            tenantConfigurationContextProvider,
            tenantSpecificDbContextProvider,
            encryptionProvider,
            sandboxProvisioner,
            CreateOptions(),
            configuration,
            new DbConnectionStringBuilderAdapterFactory(new SqlConnectionStringBuilderAdapter()));

        await job.Execute(CreateJobExecutionContext(dbInstance.Id));

        adminApiContext.DbInstances.Single().DatabaseName.ShouldBe("EdFi_Ods_My_District_Minimal");
    }

    [Test]
    public async Task Execute_ReusesExistingDatabaseName_WhenAlreadyAssigned()
    {
        var configuration = CreateConfiguration();
        using var adminApiContext = CreateAdminApiContext($"Admin_{Guid.NewGuid()}", configuration);
        using var usersContext = CreateUsersContext($"Users_{Guid.NewGuid()}");
        var jobStatusService = A.Fake<IJobStatusService>();
        var tenantConfigurationProvider = CreateTenantConfigurationProvider();
        var tenantConfigurationContextProvider = A.Fake<IContextProvider<TenantConfiguration>>();
        var tenantSpecificDbContextProvider = A.Fake<ITenantSpecificDbContextProvider>();
        var encryptionProvider = A.Fake<ISymmetricStringEncryptionProvider>();
        var sandboxProvisioner = A.Fake<ISandboxProvisioner>();
        const string existingDatabaseName = "Existing_Database_Name";
        string plaintextConnectionString = null;

        A.CallTo(() => encryptionProvider.Encrypt(A<string>._, A<byte[]>._))
            .Invokes((string connectionString, byte[] _) => plaintextConnectionString = connectionString)
            .ReturnsLazily((string connectionString, byte[] _) => $"encrypted::{connectionString}");

        var dbInstance = new Common.Infrastructure.Models.DbInstance
        {
            Name = "Sandbox",
            DatabaseTemplate = "Minimal",
            DatabaseName = existingDatabaseName,
            Status = DbInstanceStatus.PendingCreate.ToString(),
            LastRefreshed = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        adminApiContext.DbInstances.Add(dbInstance);
        adminApiContext.SaveChanges();

        var job = new CreateInstanceJob(
            A.Fake<ILogger<CreateInstanceJob>>(),
            jobStatusService,
            adminApiContext,
            usersContext,
            tenantConfigurationProvider,
            tenantConfigurationContextProvider,
            tenantSpecificDbContextProvider,
            encryptionProvider,
            sandboxProvisioner,
            CreateOptions(),
            configuration,
            new DbConnectionStringBuilderAdapterFactory(new SqlConnectionStringBuilderAdapter()));

        await job.Execute(CreateJobExecutionContext(dbInstance.Id));

        adminApiContext.DbInstances.Single().DatabaseName.ShouldBe(existingDatabaseName);
        plaintextConnectionString.ShouldContain($"Initial Catalog={existingDatabaseName}");
        A.CallTo(() => sandboxProvisioner.AddSandboxAsync(existingDatabaseName, SandboxType.Minimal))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Execute_DoesNothing_WhenDbInstanceIsNotPending()
    {
        var configuration = CreateConfiguration();
        using var adminApiContext = CreateAdminApiContext($"Admin_{Guid.NewGuid()}", configuration);
        using var usersContext = CreateUsersContext($"Users_{Guid.NewGuid()}");
        var jobStatusService = A.Fake<IJobStatusService>();
        var tenantConfigurationProvider = CreateTenantConfigurationProvider();
        var tenantConfigurationContextProvider = A.Fake<IContextProvider<TenantConfiguration>>();
        var tenantSpecificDbContextProvider = A.Fake<ITenantSpecificDbContextProvider>();
        var encryptionProvider = A.Fake<ISymmetricStringEncryptionProvider>();
        var sandboxProvisioner = A.Fake<ISandboxProvisioner>();

        var dbInstance = new Common.Infrastructure.Models.DbInstance
        {
            Name = "Sandbox",
            DatabaseTemplate = "Minimal",
            Status = DbInstanceStatus.Created.ToString(),
            LastRefreshed = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        adminApiContext.DbInstances.Add(dbInstance);
        adminApiContext.SaveChanges();

        var job = new CreateInstanceJob(
            A.Fake<ILogger<CreateInstanceJob>>(),
            jobStatusService,
            adminApiContext,
            usersContext,
            tenantConfigurationProvider,
            tenantConfigurationContextProvider,
            tenantSpecificDbContextProvider,
            encryptionProvider,
            sandboxProvisioner,
            CreateOptions(),
            configuration,
            new DbConnectionStringBuilderAdapterFactory(new SqlConnectionStringBuilderAdapter()));

        await job.Execute(CreateJobExecutionContext(dbInstance.Id));

        adminApiContext.DbInstances.Single().Status.ShouldBe(DbInstanceStatus.Created.ToString());
        usersContext.OdsInstances.ShouldBeEmpty();
        A.CallTo(() => sandboxProvisioner.AddSandboxAsync(A<string>._, A<SandboxType>._)).MustNotHaveHappened();
        A.CallTo(() => encryptionProvider.Encrypt(A<string>._, A<byte[]>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Execute_SetsDbInstanceToError_When_ProvisioningFails()
    {
        var configuration = CreateConfiguration();
        using var adminApiContext = CreateAdminApiContext($"Admin_{Guid.NewGuid()}", configuration);
        using var usersContext = CreateUsersContext($"Users_{Guid.NewGuid()}");
        var jobStatusService = A.Fake<IJobStatusService>();
        var tenantConfigurationProvider = CreateTenantConfigurationProvider();
        var tenantConfigurationContextProvider = A.Fake<IContextProvider<TenantConfiguration>>();
        var tenantSpecificDbContextProvider = A.Fake<ITenantSpecificDbContextProvider>();
        var encryptionProvider = A.Fake<ISymmetricStringEncryptionProvider>();
        var sandboxProvisioner = A.Fake<ISandboxProvisioner>();

        A.CallTo(() => sandboxProvisioner.AddSandboxAsync(A<string>._, A<SandboxType>._))
            .Throws(new InvalidOperationException("Provisioning failed."));

        var dbInstance = new Common.Infrastructure.Models.DbInstance
        {
            Name = "Sandbox",
            DatabaseTemplate = "Minimal",
            Status = DbInstanceStatus.PendingCreate.ToString(),
            LastRefreshed = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        adminApiContext.DbInstances.Add(dbInstance);
        adminApiContext.SaveChanges();

        var job = new CreateInstanceJob(
            A.Fake<ILogger<CreateInstanceJob>>(),
            jobStatusService,
            adminApiContext,
            usersContext,
            tenantConfigurationProvider,
            tenantConfigurationContextProvider,
            tenantSpecificDbContextProvider,
            encryptionProvider,
            sandboxProvisioner,
            CreateOptions(),
            configuration,
            new DbConnectionStringBuilderAdapterFactory(new SqlConnectionStringBuilderAdapter()));

        await job.Execute(CreateJobExecutionContext(dbInstance.Id));

        adminApiContext.DbInstances.Single().Status.ShouldBe(DbInstanceStatus.CreateFailed.ToString());
        usersContext.OdsInstances.ShouldBeEmpty();
        A.CallTo(() => jobStatusService.SetStatusAsync(A<string>._, QuartzJobStatus.Error, A<string>._, A<string>.That.Contains("Provisioning failed.")))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Execute_SetsDbInstanceToError_WhenPendingStateAlreadyContainsOdsReferences()
    {
        var configuration = CreateConfiguration();
        using var adminApiContext = CreateAdminApiContext($"Admin_{Guid.NewGuid()}", configuration);
        using var usersContext = CreateUsersContext($"Users_{Guid.NewGuid()}");
        var jobStatusService = A.Fake<IJobStatusService>();
        var tenantConfigurationProvider = CreateTenantConfigurationProvider();
        var tenantConfigurationContextProvider = A.Fake<IContextProvider<TenantConfiguration>>();
        var tenantSpecificDbContextProvider = A.Fake<ITenantSpecificDbContextProvider>();
        var encryptionProvider = A.Fake<ISymmetricStringEncryptionProvider>();
        var sandboxProvisioner = A.Fake<ISandboxProvisioner>();

        var dbInstance = new Common.Infrastructure.Models.DbInstance
        {
            Name = "Sandbox",
            DatabaseTemplate = "Minimal",
            Status = DbInstanceStatus.PendingCreate.ToString(),
            OdsInstanceId = 42,
            OdsInstanceName = "Sandbox",
            LastRefreshed = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        adminApiContext.DbInstances.Add(dbInstance);
        adminApiContext.SaveChanges();

        var job = new CreateInstanceJob(
            A.Fake<ILogger<CreateInstanceJob>>(),
            jobStatusService,
            adminApiContext,
            usersContext,
            tenantConfigurationProvider,
            tenantConfigurationContextProvider,
            tenantSpecificDbContextProvider,
            encryptionProvider,
            sandboxProvisioner,
            CreateOptions(),
            configuration,
            new DbConnectionStringBuilderAdapterFactory(new SqlConnectionStringBuilderAdapter()));

        await job.Execute(CreateJobExecutionContext(dbInstance.Id));

        adminApiContext.DbInstances.Single().Status.ShouldBe(DbInstanceStatus.CreateFailed.ToString());
        usersContext.OdsInstances.ShouldBeEmpty();
        A.CallTo(() => sandboxProvisioner.AddSandboxAsync(A<string>._, A<SandboxType>._)).MustNotHaveHappened();
        A.CallTo(() => jobStatusService.SetStatusAsync(A<string>._, QuartzJobStatus.Error, A<string>._, A<string>.That.Contains("invalid pending state")))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Execute_UsesTenantSpecificOdsConnectionString_WhenMultiTenancyIsEnabled()
    {
        var configuration = CreateConfiguration();
        using var defaultAdminApiContext = CreateAdminApiContext($"Admin_Default_{Guid.NewGuid()}", configuration);
        using var defaultUsersContext = CreateUsersContext($"Users_Default_{Guid.NewGuid()}");
        using var tenantAdminApiContext = CreateAdminApiContext($"Admin_Tenant_{Guid.NewGuid()}", configuration);
        using var tenantUsersContext = CreateUsersContext($"Users_Tenant_{Guid.NewGuid()}");
        var jobStatusService = A.Fake<IJobStatusService>();
        var tenantConfigurationProvider = CreateTenantConfigurationProvider("tenant1");
        var tenantConfigurationContextProvider = A.Fake<IContextProvider<TenantConfiguration>>();
        var tenantSpecificDbContextProvider = A.Fake<ITenantSpecificDbContextProvider>();
        var encryptionProvider = A.Fake<ISymmetricStringEncryptionProvider>();
        var sandboxProvisioner = A.Fake<ISandboxProvisioner>();
        string plaintextConnectionString = null;

        A.CallTo(() => tenantSpecificDbContextProvider.GetAdminApiDbContext("tenant1"))
            .Returns(tenantAdminApiContext);
        A.CallTo(() => tenantSpecificDbContextProvider.GetUsersContext("tenant1"))
            .Returns(tenantUsersContext);
        A.CallTo(() => encryptionProvider.Encrypt(A<string>._, A<byte[]>._))
            .Invokes((string connectionString, byte[] _) => plaintextConnectionString = connectionString)
            .ReturnsLazily((string connectionString, byte[] _) => $"encrypted::{connectionString}");

        var dbInstance = new Common.Infrastructure.Models.DbInstance
        {
            Name = "Sandbox",
            DatabaseTemplate = "Sample",
            Status = DbInstanceStatus.PendingCreate.ToString(),
            LastRefreshed = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        tenantAdminApiContext.DbInstances.Add(dbInstance);
        tenantAdminApiContext.SaveChanges();

        var job = new CreateInstanceJob(
            A.Fake<ILogger<CreateInstanceJob>>(),
            jobStatusService,
            defaultAdminApiContext,
            defaultUsersContext,
            tenantConfigurationProvider,
            tenantConfigurationContextProvider,
            tenantSpecificDbContextProvider,
            encryptionProvider,
            sandboxProvisioner,
            CreateOptions(multiTenancy: true),
            configuration,
            new DbConnectionStringBuilderAdapterFactory(new SqlConnectionStringBuilderAdapter()));

        await job.Execute(CreateJobExecutionContext(dbInstance.Id, "tenant1"));

        var persistedDbInstance = tenantAdminApiContext.DbInstances.Single();
        var persistedOdsInstance = tenantUsersContext.OdsInstances.Single();
        const string expectedDatabaseName = "EdFi_Ods_Sandbox_Sample";

        persistedDbInstance.Status.ShouldBe(DbInstanceStatus.Created.ToString());
        persistedDbInstance.DatabaseName.ShouldBe(expectedDatabaseName);
        persistedOdsInstance.InstanceType.ShouldBe("Sample");
        plaintextConnectionString.ShouldNotBeNull();
        plaintextConnectionString.ShouldContain($"Initial Catalog={expectedDatabaseName}");
        plaintextConnectionString.ShouldNotContain("Initial Catalog=EdFi_Admin");
        plaintextConnectionString.ShouldNotContain("Initial Catalog=TenantTemplateDb");
        A.CallTo(() => tenantConfigurationContextProvider.Set(A<TenantConfiguration>.That.Matches(tc => tc != null && tc.TenantIdentifier == "tenant1")))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => tenantConfigurationContextProvider.Set(null))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => sandboxProvisioner.AddSandboxAsync(expectedDatabaseName, SandboxType.Sample))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Execute_ReusesExistingOdsInstance_WhenFinalNameAlreadyExists()
    {
        var configuration = CreateConfiguration();
        using var adminApiContext = CreateAdminApiContext($"Admin_{Guid.NewGuid()}", configuration);
        using var usersContext = CreateUsersContext($"Users_{Guid.NewGuid()}");
        var jobStatusService = A.Fake<IJobStatusService>();
        var tenantConfigurationProvider = CreateTenantConfigurationProvider();
        var tenantConfigurationContextProvider = A.Fake<IContextProvider<TenantConfiguration>>();
        var tenantSpecificDbContextProvider = A.Fake<ITenantSpecificDbContextProvider>();
        var encryptionProvider = A.Fake<ISymmetricStringEncryptionProvider>();
        var sandboxProvisioner = A.Fake<ISandboxProvisioner>();
        var encryptedConnectionString = "encrypted::updated";

        A.CallTo(() => encryptionProvider.Encrypt(A<string>._, A<byte[]>._))
            .Returns(encryptedConnectionString);

        var dbInstance = new Common.Infrastructure.Models.DbInstance
        {
            Name = "Sandbox",
            DatabaseTemplate = "Minimal",
            Status = DbInstanceStatus.PendingCreate.ToString(),
            LastRefreshed = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };

        adminApiContext.DbInstances.Add(dbInstance);
        adminApiContext.SaveChanges();

        usersContext.OdsInstances.Add(new OdsInstance
        {
            Name = "Sandbox",
            InstanceType = "Minimal",
            ConnectionString = "encrypted::existing"
        });
        usersContext.SaveChanges();

        var job = new CreateInstanceJob(
            A.Fake<ILogger<CreateInstanceJob>>(),
            jobStatusService,
            adminApiContext,
            usersContext,
            tenantConfigurationProvider,
            tenantConfigurationContextProvider,
            tenantSpecificDbContextProvider,
            encryptionProvider,
            sandboxProvisioner,
            CreateOptions(),
            configuration,
            new DbConnectionStringBuilderAdapterFactory(new SqlConnectionStringBuilderAdapter()));

        await job.Execute(CreateJobExecutionContext(dbInstance.Id));

        var persistedDbInstance = adminApiContext.DbInstances.Single();
        var persistedOdsInstance = usersContext.OdsInstances.Single();
        const string expectedDatabaseName = "EdFi_Ods_Sandbox_Minimal";

        persistedDbInstance.Status.ShouldBe(DbInstanceStatus.Created.ToString());
        persistedDbInstance.DatabaseName.ShouldBe(expectedDatabaseName);
        persistedDbInstance.OdsInstanceId.ShouldBe(persistedOdsInstance.OdsInstanceId);
        persistedDbInstance.OdsInstanceName.ShouldBe("Sandbox");
        persistedOdsInstance.ConnectionString.ShouldBe(encryptedConnectionString);
        usersContext.OdsInstances.Count().ShouldBe(1);
        A.CallTo(() => sandboxProvisioner.AddSandboxAsync(expectedDatabaseName, SandboxType.Minimal))
            .MustHaveHappenedOnceExactly();
    }
}
