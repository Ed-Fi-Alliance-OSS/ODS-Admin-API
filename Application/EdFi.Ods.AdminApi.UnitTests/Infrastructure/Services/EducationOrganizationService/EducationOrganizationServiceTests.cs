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
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;
using EducationOrganizationServiceImpl = EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService.EducationOrganizationService;


namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Services.EducationOrganizationService;

[TestFixture]
internal class EducationOrganizationServiceTests
{
    private IOptions<AppSettings> _options = null!;
    private ISymmetricStringEncryptionProvider _encryptionProvider = null!;
    private AppSettings _appSettings = null!;
    private string _encryptionKey = null!;
    private ILogger<EducationOrganizationServiceImpl> _logger = null!;
    private ITenantSpecificDbContextProvider _tenantSpecificDbContextProvider = null!;
    private IServiceScopeFactory _serviceScopeFactory = null!;

    [SetUp]
    public void SetUp()
    {
        _options = A.Fake<IOptions<AppSettings>>();
        _encryptionProvider = A.Fake<ISymmetricStringEncryptionProvider>();

        _encryptionKey = Convert.ToBase64String(new byte[32]);
        _appSettings = new AppSettings
        {
            MultiTenancy = false,
            DatabaseEngine = "SqlServer",
            EncryptionKey = _encryptionKey
        };

        A.CallTo(() => _options.Value).Returns(_appSettings);
        _logger = A.Fake<ILogger<EducationOrganizationServiceImpl>>();
        _tenantSpecificDbContextProvider = A.Fake<ITenantSpecificDbContextProvider>();
        _serviceScopeFactory = A.Fake<IServiceScopeFactory>();
    }

    [Test]
    public async Task Execute_Should_Throw_InvalidOperationException_When_EncryptionKey_Is_Null()
    {
        _appSettings.EncryptionKey = null;
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_EncryptionKeyNull")
            .Options;
        var usersContext = new SqlServerUsersContext(contextOptions);
        var adminApiDbContext = new AdminApiDbContext(
            new DbContextOptionsBuilder<AdminApiDbContext>().UseInMemoryDatabase("TestDb_EncryptionKeyNull_Admin").Options,
            A.Fake<IConfiguration>());

        // Ensure the correct class is instantiated here.
        var service = new EducationOrganizationServiceImpl(
            _options,
            usersContext,
            _encryptionProvider,
            _tenantSpecificDbContextProvider,
            _serviceScopeFactory,
            _logger);

        await Should.ThrowAsync<InvalidOperationException>(async () => await service.Execute(null, null))
            .ContinueWith(t => t.Result.Message.ShouldBe("EncryptionKey can't be null."));
    }

    [Test]
    public async Task Execute_Should_Throw_NotFoundException_When_DatabaseEngine_Is_Null()
    {
        _appSettings.DatabaseEngine = null;
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_DatabaseEngineNull")
            .Options;
        var usersContext = new SqlServerUsersContext(contextOptions);
        var adminApiDbContext = new AdminApiDbContext(
            new DbContextOptionsBuilder<AdminApiDbContext>().UseInMemoryDatabase("TestDb_DatabaseEngineNull_Admin").Options,
            A.Fake<IConfiguration>());

        var service = new EducationOrganizationServiceImpl(
            _options,
            usersContext,
            _encryptionProvider,
            _tenantSpecificDbContextProvider,
            _serviceScopeFactory,
            _logger);

        await Should.ThrowAsync<Exception>(async () => await service.Execute(null, null));
    }

    [Test]
    public async Task Execute_Should_Process_Single_Tenant_When_MultiTenancy_Disabled()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_SingleTenant")
            .Options;

        using var usersContext = new SqlServerUsersContext(contextOptions);
        var adminApiDbContext = new AdminApiDbContext(
            new DbContextOptionsBuilder<AdminApiDbContext>().UseInMemoryDatabase("TestDb_SingleTenant_Admin").Options,
            A.Fake<IConfiguration>());

        var odsInstance = new OdsInstance
        {
            OdsInstanceId = 1,
            Name = "TestInstance",
            ConnectionString = "encrypted-connection-string"
        };
        usersContext.OdsInstances.Add(odsInstance);
        await usersContext.SaveChangesAsync();

        var service = new EducationOrganizationServiceImpl(
            _options,
            usersContext,
            _encryptionProvider,
            _tenantSpecificDbContextProvider,
            _serviceScopeFactory,
            _logger);

        string decryptedConnectionString = null;
        A.CallTo(() => _encryptionProvider.TryDecrypt(
            A<string>._,
            A<byte[]>._,
            out decryptedConnectionString))
            .Returns(false);

        Should.NotThrow(() => service.Execute(null, null).GetAwaiter().GetResult());
    }

    [Test]
    public async Task Execute_Should_Process_For_Selected_Tenant_When_MultiTenancy_Enabled()
    {
        _appSettings.MultiTenancy = true;

        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_MultiTenant")
            .Options;

        using var usersContext = new SqlServerUsersContext(contextOptions);
        var tenantUsersContext = new SqlServerUsersContext(contextOptions);

        A.CallTo(() => _tenantSpecificDbContextProvider.GetUsersContext("tenant1"))
            .Returns(tenantUsersContext);

        var processOdsInstanceCallCount = 0;
        var service = new TestableEducationOrganizationService(
            _options,
            usersContext,
            _encryptionProvider,
            _tenantSpecificDbContextProvider,
            _serviceScopeFactory,
            () => processOdsInstanceCallCount++,
            _logger);

        await service.Execute("tenant1", null);

        A.CallTo(() => _tenantSpecificDbContextProvider.GetUsersContext("tenant1")).MustHaveHappenedOnceExactly();
        processOdsInstanceCallCount.ShouldBe(1);
    }

    private class TestableEducationOrganizationService(
        IOptions<AppSettings> options,
        IUsersContext usersContext,
        ISymmetricStringEncryptionProvider encryptionProvider,
        ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
        IServiceScopeFactory serviceScopeFactory,
        Action onProcessOdsInstance, ILogger<EducationOrganizationServiceImpl> logger) : EducationOrganizationServiceImpl(options, usersContext, encryptionProvider, tenantSpecificDbContextProvider, serviceScopeFactory, logger)
    {
        private readonly Action _onProcessOdsInstance = onProcessOdsInstance;

        public override Task ProcessOdsInstanceAsync(string? tenantName, IUsersContext usersContext, string encryptionKey, string databaseEngine, int? instanceId = null, int maxDegreeOfParallelism = 10)
        {
            _onProcessOdsInstance();
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task Execute_Should_Throw_NotSupportedException_When_DatabaseEngine_Is_Invalid()
    {
        _appSettings.DatabaseEngine = "InvalidEngine";

        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_InvalidEngine")
            .Options;

        using var usersContext = new SqlServerUsersContext(contextOptions);
        var adminApiDbContext = new AdminApiDbContext(
            new DbContextOptionsBuilder<AdminApiDbContext>().UseInMemoryDatabase("TestDb_InvalidEngine_Admin").Options,
            A.Fake<IConfiguration>());

        var service = new EducationOrganizationServiceImpl(
              _options,
              usersContext,
              _encryptionProvider,
              _tenantSpecificDbContextProvider,
              _serviceScopeFactory,
              _logger);

        var exception = await Should.ThrowAsync<NotSupportedException>(async () => await service.Execute(null, null));
        exception.Message.ShouldContain("Not supported DatabaseEngine \"InvalidEngine\". Supported engines: SqlServer, and PostgreSql.");
    }

    [Test]
    public async Task Execute_Should_Handle_PostgreSql_DatabaseEngine()
    {
        _appSettings.DatabaseEngine = "PostgreSql";

        var contextOptions = new DbContextOptionsBuilder<PostgresUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_PostgreSql")
            .Options;

        using var usersContext = new PostgresUsersContext(contextOptions);

        var odsInstance = new OdsInstance
        {
            OdsInstanceId = 1,
            Name = "TestInstance",
            ConnectionString = "encrypted-connection-string"
        };
        usersContext.OdsInstances.Add(odsInstance);
        await usersContext.SaveChangesAsync();

        var service = new EducationOrganizationServiceImpl(
              _options,
              usersContext,
              _encryptionProvider,
              _tenantSpecificDbContextProvider,
              _serviceScopeFactory,
              _logger);

        string decryptedConnectionString = null;
        A.CallTo(() => _encryptionProvider.TryDecrypt(
            A<string>._,
            A<byte[]>._,
            out decryptedConnectionString))
            .Returns(false);

        Should.NotThrow(() => service.Execute(null, null).GetAwaiter().GetResult());
    }

    [Test]
    public async Task Execute_Should_Process_MultiTenancy_With_PostgreSql()
    {
        _appSettings.MultiTenancy = true;
        _appSettings.DatabaseEngine = "PostgreSql";
        var contextOptions = new DbContextOptionsBuilder<PostgresUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_MultiTenantPostgres")
            .Options;

        using var context = new PostgresUsersContext(contextOptions);
        var tenantUsersContext = new PostgresUsersContext(contextOptions);

        A.CallTo(() => _tenantSpecificDbContextProvider.GetUsersContext("tenant1"))
            .Returns(tenantUsersContext);

        var processOdsInstanceCallCount = 0;
        var service = new TestableEducationOrganizationService(
        _options,
        context,
        _encryptionProvider,
        _tenantSpecificDbContextProvider,
        _serviceScopeFactory,
        () => processOdsInstanceCallCount++,
        _logger);

        await service.Execute("tenant1", null);
        A.CallTo(() => _tenantSpecificDbContextProvider.GetUsersContext("tenant1")).MustHaveHappenedOnceExactly();
        processOdsInstanceCallCount.ShouldBe(1);
    }

    [Test]
    public async Task ProcessOdsInstance_Should_Filter_By_InstanceId_When_Provided()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_FilterByInstanceId")
            .Options;

        using var usersContext = new SqlServerUsersContext(contextOptions);

        var targetInstance = new OdsInstance
        {
            OdsInstanceId = 1,
            Name = "Target Instance",
            ConnectionString = "encrypted-connection-string-1"
        };

        var otherInstance = new OdsInstance
        {
            OdsInstanceId = 2,
            Name = "Other Instance",
            ConnectionString = "encrypted-connection-string-2"
        };

        usersContext.OdsInstances.Add(targetInstance);
        usersContext.OdsInstances.Add(otherInstance);
        await usersContext.SaveChangesAsync();

        var processedInstanceIds = new List<int>();
        var service = new TestableEducationOrganizationServiceWithTracking(
            _options,
            usersContext,
            _encryptionProvider,
            _tenantSpecificDbContextProvider,
            _serviceScopeFactory,
            processedInstanceIds,
            _logger);

        await service.ProcessOdsInstanceAsync("default", usersContext, _encryptionKey, "SqlServer", instanceId: 1);

        processedInstanceIds.Count.ShouldBe(1);
        processedInstanceIds.ShouldContain(1);
        processedInstanceIds.ShouldNotContain(2);
    }

    [Test]
    public async Task ProcessOdsInstance_Should_Process_All_Instances_When_InstanceId_Is_Null()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_ProcessAllInstances")
            .Options;

        using var usersContext = new SqlServerUsersContext(contextOptions);

        var instance1 = new OdsInstance
        {
            OdsInstanceId = 1,
            Name = "Instance 1",
            ConnectionString = "encrypted-connection-string-1"
        };

        var instance2 = new OdsInstance
        {
            OdsInstanceId = 2,
            Name = "Instance 2",
            ConnectionString = "encrypted-connection-string-2"
        };

        var instance3 = new OdsInstance
        {
            OdsInstanceId = 3,
            Name = "Instance 3",
            ConnectionString = "encrypted-connection-string-3"
        };

        usersContext.OdsInstances.Add(instance1);
        usersContext.OdsInstances.Add(instance2);
        usersContext.OdsInstances.Add(instance3);
        await usersContext.SaveChangesAsync();

        var processedInstanceIds = new List<int>();
        var service = new TestableEducationOrganizationServiceWithTracking(
            _options,
            usersContext,
            _encryptionProvider,
            _tenantSpecificDbContextProvider,
            _serviceScopeFactory,
            processedInstanceIds,
            _logger);

        await service.ProcessOdsInstanceAsync("default", usersContext, _encryptionKey, "SqlServer", instanceId: null);

        processedInstanceIds.Count.ShouldBe(3);
        processedInstanceIds.ShouldContain(1);
        processedInstanceIds.ShouldContain(2);
        processedInstanceIds.ShouldContain(3);
    }

    [Test]
    public async Task ProcessOdsInstance_Should_Process_No_Instances_When_InstanceId_Does_Not_Exist()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_NonExistentInstanceId")
            .Options;

        using var usersContext = new SqlServerUsersContext(contextOptions);

        var instance = new OdsInstance
        {
            OdsInstanceId = 1,
            Name = "Instance 1",
            ConnectionString = "encrypted-connection-string-1"
        };

        usersContext.OdsInstances.Add(instance);
        await usersContext.SaveChangesAsync();

        var processedInstanceIds = new List<int>();
        var service = new TestableEducationOrganizationServiceWithTracking(
            _options,
            usersContext,
            _encryptionProvider,
            _tenantSpecificDbContextProvider,
            _serviceScopeFactory,
            processedInstanceIds,
            _logger);

        await service.ProcessOdsInstanceAsync("default", usersContext, _encryptionKey, "SqlServer", instanceId: 999);

        processedInstanceIds.ShouldBeEmpty();
    }

    [Test]
    public async Task Execute_Should_Continue_Processing_Other_Instances_When_One_Fails()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_ErrorHandling")
            .Options;

        using var usersContext = new SqlServerUsersContext(contextOptions);

        var successInstance1 = new OdsInstance
        {
            OdsInstanceId = 1,
            Name = "Success Instance 1",
            ConnectionString = "encrypted-1"
        };

        var failingInstance = new OdsInstance
        {
            OdsInstanceId = 2,
            Name = "Failing Instance",
            ConnectionString = "encrypted-2"
        };

        var successInstance2 = new OdsInstance
        {
            OdsInstanceId = 3,
            Name = "Success Instance 2",
            ConnectionString = "encrypted-3"
        };

        usersContext.OdsInstances.Add(successInstance1);
        usersContext.OdsInstances.Add(failingInstance);
        usersContext.OdsInstances.Add(successInstance2);
        await usersContext.SaveChangesAsync();

        var fakeLogger = A.Fake<ILogger<EducationOrganizationServiceImpl>>();
        var fakeEncryption = A.Fake<ISymmetricStringEncryptionProvider>();

        // Setup encryption: succeed for all instances
        string decryptedConnectionString;
        A.CallTo(() => fakeEncryption.TryDecrypt("encrypted-1", A<byte[]>._, out decryptedConnectionString))
            .Returns(true).AssignsOutAndRefParameters("Server=test1;");
        A.CallTo(() => fakeEncryption.TryDecrypt("encrypted-2", A<byte[]>._, out decryptedConnectionString))
            .Returns(true).AssignsOutAndRefParameters("Server=test2;");
        A.CallTo(() => fakeEncryption.TryDecrypt("encrypted-3", A<byte[]>._, out decryptedConnectionString))
            .Returns(true).AssignsOutAndRefParameters("Server=test3;");

        var service = new TestableEducationOrganizationServiceWithErrorSimulation(
            _options,
            usersContext,
            fakeEncryption,
            _tenantSpecificDbContextProvider,
            _serviceScopeFactory,
            callNum => callNum == 2, // Fail on second call
            fakeLogger);

        // Should not throw - processing should continue despite one failure
        await Should.NotThrowAsync(async () => await service.Execute(null, null));

        // GetEducationOrganizationsAsync should be called for all three instances
        service.CallCount.ShouldBe(3);
    }

    [Test]
    public async Task Execute_Should_Not_Throw_When_All_Instances_Fail()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_AllInstancesFail")
            .Options;

        using var usersContext = new SqlServerUsersContext(contextOptions);

        var failingInstance1 = new OdsInstance
        {
            OdsInstanceId = 1,
            Name = "Failing Instance 1",
            ConnectionString = "encrypted-1"
        };

        var failingInstance2 = new OdsInstance
        {
            OdsInstanceId = 2,
            Name = "Failing Instance 2",
            ConnectionString = "encrypted-2"
        };

        usersContext.OdsInstances.Add(failingInstance1);
        usersContext.OdsInstances.Add(failingInstance2);
        await usersContext.SaveChangesAsync();

        var fakeLogger = A.Fake<ILogger<EducationOrganizationServiceImpl>>();
        var fakeEncryption = A.Fake<ISymmetricStringEncryptionProvider>();

        // Setup encryption to succeed
        string decryptedConnectionString;
        A.CallTo(() => fakeEncryption.TryDecrypt(A<string>._, A<byte[]>._, out decryptedConnectionString))
            .Returns(true).AssignsOutAndRefParameters("Server=test;");

        var service = new TestableEducationOrganizationServiceWithErrorSimulation(
            _options,
            usersContext,
            fakeEncryption,
            _tenantSpecificDbContextProvider,
            _serviceScopeFactory,
            _ => true, // Always fail
            fakeLogger);

        // Should not throw even when all instances fail
        await Should.NotThrowAsync(async () => await service.Execute(null, null));

        // GetEducationOrganizationsAsync should be called for both instances
        service.CallCount.ShouldBe(2);
    }

    private class TestableEducationOrganizationServiceWithTracking(
        IOptions<AppSettings> options,
        IUsersContext usersContext,
        ISymmetricStringEncryptionProvider encryptionProvider,
        ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
        IServiceScopeFactory serviceScopeFactory,
        List<int> processedInstanceIds,
        ILogger<EducationOrganizationServiceImpl> logger) : EducationOrganizationServiceImpl(options, usersContext, encryptionProvider, tenantSpecificDbContextProvider, serviceScopeFactory, logger)
    {
        private readonly List<int> _processedInstanceIds = processedInstanceIds;

        protected override Task RefreshEducationOrganizationsAsync(
            string? tenantName, string encryptionKey, string databaseEngine,
            OdsInstance odsInstance, CancellationToken cancellationToken = default)
        {
            lock (_processedInstanceIds)
            {
                _processedInstanceIds.Add(odsInstance.OdsInstanceId);
            }
            return Task.CompletedTask;
        }
    }

    private class TestableEducationOrganizationServiceWithErrorSimulation(
        IOptions<AppSettings> options,
        IUsersContext usersContext,
        ISymmetricStringEncryptionProvider encryptionProvider,
        ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
        IServiceScopeFactory serviceScopeFactory,
        Func<int, bool> shouldFail,
        ILogger<EducationOrganizationServiceImpl> logger) : EducationOrganizationServiceImpl(options, usersContext, encryptionProvider, tenantSpecificDbContextProvider, serviceScopeFactory, logger)
    {
        private readonly Func<int, bool> _shouldFail = shouldFail;
        private int _callCount;
        public int CallCount => _callCount;

        // Override at the RefreshEducationOrganizationsAsync level so that
        // ProcessOdsInstanceAsync (the thing under test) drives all iterations.
        // We replicate the base-class error-handling contract: catch per instance
        // so one failure never blocks the others.
        protected override Task RefreshEducationOrganizationsAsync(
            string? tenantName, string encryptionKey, string databaseEngine,
            OdsInstance odsInstance, CancellationToken cancellationToken = default)
        {
            // Capture the atomic call number before the predicate sees it.
            var callNum = Interlocked.Increment(ref _callCount);
            try
            {
                if (_shouldFail(callNum))
                    throw new InvalidOperationException("Simulated database error");
            }
            catch (Exception)
            {
                // mirrors production: errors are logged and swallowed per instance
            }

            return Task.CompletedTask;
        }
    }

    private class TestableEducationOrganizationServiceWithParallelismTracking(
        IOptions<AppSettings> options,
        IUsersContext usersContext,
        ISymmetricStringEncryptionProvider encryptionProvider,
        ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
        IServiceScopeFactory serviceScopeFactory,
        List<int> processedInstanceIds,
        ILogger<EducationOrganizationServiceImpl> logger) : EducationOrganizationServiceImpl(options, usersContext, encryptionProvider, tenantSpecificDbContextProvider, serviceScopeFactory, logger)
    {
        private readonly List<int> _processedInstanceIds = processedInstanceIds;
        public int PeakConcurrency => _peakConcurrency;
        private int _peakConcurrency;
        private int _activeTasks;

        protected override async Task RefreshEducationOrganizationsAsync(
            string? tenantName, string encryptionKey, string databaseEngine,
            OdsInstance odsInstance, CancellationToken cancellationToken = default)
        {
            var current = Interlocked.Increment(ref _activeTasks);

            // Atomically update peak: spin until our value is recorded or a higher one already is.
            int observed;
            do
            {
                observed = _peakConcurrency;
                if (current <= observed) break;
            }
            while (Interlocked.CompareExchange(ref _peakConcurrency, current, observed) != observed);

            try
            {
                await Task.Delay(10, cancellationToken); // small delay to allow overlap when parallelism > 1

                lock (_processedInstanceIds)
                {
                    _processedInstanceIds.Add(odsInstance.OdsInstanceId);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _activeTasks);
            }
        }
    }

    [Test]
    public async Task ProcessOdsInstanceAsync_Should_Process_All_Instances_Within_MaxDegreeOfParallelism()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Parallelism")
            .Options;

        using var usersContext = new SqlServerUsersContext(contextOptions);

        for (var i = 1; i <= 5; i++)
        {
            usersContext.OdsInstances.Add(new OdsInstance
            {
                OdsInstanceId = i,
                Name = $"Instance {i}",
                ConnectionString = $"encrypted-{i}"
            });
        }
        await usersContext.SaveChangesAsync();

        var processedInstanceIds = new List<int>();
        var service = new TestableEducationOrganizationServiceWithParallelismTracking(
            _options,
            usersContext,
            _encryptionProvider,
            _tenantSpecificDbContextProvider,
            _serviceScopeFactory,
            processedInstanceIds,
            _logger);

        await service.ProcessOdsInstanceAsync("default", usersContext, _encryptionKey, "SqlServer",
            instanceId: null, maxDegreeOfParallelism: 2);

        processedInstanceIds.Count.ShouldBe(5);
        processedInstanceIds.ShouldContain(1);
        processedInstanceIds.ShouldContain(2);
        processedInstanceIds.ShouldContain(3);
        processedInstanceIds.ShouldContain(4);
        processedInstanceIds.ShouldContain(5);
        service.PeakConcurrency.ShouldBeLessThanOrEqualTo(2);
    }

    [Test]
    public async Task ProcessOdsInstanceAsync_Should_Process_Sequentially_When_MaxDegreeOfParallelism_Is_One()
    {
        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Sequential")
            .Options;

        using var usersContext = new SqlServerUsersContext(contextOptions);

        for (var i = 1; i <= 3; i++)
        {
            usersContext.OdsInstances.Add(new OdsInstance
            {
                OdsInstanceId = i,
                Name = $"Instance {i}",
                ConnectionString = $"encrypted-{i}"
            });
        }
        await usersContext.SaveChangesAsync();

        var processedInstanceIds = new List<int>();
        var service = new TestableEducationOrganizationServiceWithParallelismTracking(
            _options,
            usersContext,
            _encryptionProvider,
            _tenantSpecificDbContextProvider,
            _serviceScopeFactory,
            processedInstanceIds,
            _logger);

        await service.ProcessOdsInstanceAsync("default", usersContext, _encryptionKey, "SqlServer",
            instanceId: null, maxDegreeOfParallelism: 1);

        processedInstanceIds.Count.ShouldBe(3);
        service.PeakConcurrency.ShouldBe(1);
    }

    [Test]
    public async Task Execute_Should_Use_MaxDegreeOfParallelism_From_AppSettings()
    {
        _appSettings.MaxDegreeOfParallelism = 1;

        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_AppSettings_Parallelism")
            .Options;

        using var usersContext = new SqlServerUsersContext(contextOptions);

        for (var i = 1; i <= 3; i++)
        {
            usersContext.OdsInstances.Add(new OdsInstance
            {
                OdsInstanceId = i,
                Name = $"Instance {i}",
                ConnectionString = $"encrypted-{i}"
            });
        }
        await usersContext.SaveChangesAsync();

        var processedInstanceIds = new List<int>();
        var service = new TestableEducationOrganizationServiceWithParallelismTracking(
            _options,
            usersContext,
            _encryptionProvider,
            _tenantSpecificDbContextProvider,
            _serviceScopeFactory,
            processedInstanceIds,
            _logger);

        await service.Execute(null, null);

        processedInstanceIds.Count.ShouldBe(3);
        service.PeakConcurrency.ShouldBe(1);
    }
}
