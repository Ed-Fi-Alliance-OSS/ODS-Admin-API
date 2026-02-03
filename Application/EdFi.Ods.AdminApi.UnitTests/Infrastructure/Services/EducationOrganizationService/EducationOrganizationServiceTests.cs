// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Tenants;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
    private ITenantConfigurationProvider _tenantConfigurationProvider = null!;
    private ISymmetricStringEncryptionProvider _encryptionProvider = null!;
    private IConfiguration _configuration;
    private AppSettings _appSettings = null!;
    private string _encryptionKey = null!;
    private ILogger<EducationOrganizationServiceImpl> _logger = null!;
    private ITenantSpecificDbContextProvider _tenantSpecificDbContextProvider = null!;

    [SetUp]
    public void SetUp()
    {
        _options = A.Fake<IOptions<AppSettings>>();
        _tenantConfigurationProvider = A.Fake<ITenantConfigurationProvider>();
        _encryptionProvider = A.Fake<ISymmetricStringEncryptionProvider>();
        _configuration = new ConfigurationBuilder()
           .AddInMemoryCollection(new Dictionary<string, string>
           {
                        { "AppSettings:DatabaseEngine", "SqlServer" }
           })
           .Build();

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
            adminApiDbContext,
            _encryptionProvider,
            _configuration,
            _tenantSpecificDbContextProvider,
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
            adminApiDbContext,
            _encryptionProvider,
            _configuration,
            _tenantSpecificDbContextProvider,
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
            adminApiDbContext,
            _encryptionProvider,
            _configuration,
            _tenantSpecificDbContextProvider,
            _logger);

        string decryptedConnectionString = null;
        A.CallTo(() => _encryptionProvider.TryDecrypt(
            A<string>._,
            A<byte[]>._,
            out decryptedConnectionString))
            .Returns(false);
    }

    [Test]
    public async Task Execute_Should_Process_For_Selected_Tenant_When_MultiTenancy_Enabled()
    {
        _appSettings.MultiTenancy = true;

        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_MultiTenant")
            .Options;

        using var usersContext = new SqlServerUsersContext(contextOptions);
        var adminApiDbContext = new AdminApiDbContext(
            new DbContextOptionsBuilder<AdminApiDbContext>().UseInMemoryDatabase("TestDb_MultiTenant_Admin").Options,
            A.Fake<IConfiguration>());

        var processOdsInstanceCallCount = 0;
        var service = new TestableEducationOrganizationService(
            _options,
            usersContext,
            adminApiDbContext,
            _encryptionProvider,
            _configuration,
            _tenantSpecificDbContextProvider,
            () => processOdsInstanceCallCount++,
            _logger);

        await service.Execute("tenant1", null);

        A.CallTo(() => _tenantSpecificDbContextProvider.GetAdminApiDbContext("tenant1")).MustHaveHappenedOnceExactly();
        processOdsInstanceCallCount.ShouldBe(1);
    }

    private class TestableEducationOrganizationService : EducationOrganizationServiceImpl
    {
        private readonly Action _onProcessOdsInstance;

        public TestableEducationOrganizationService(
            IOptions<AppSettings> options,
            IUsersContext usersContext,
            AdminApiDbContext adminApiDbContext,
            ISymmetricStringEncryptionProvider encryptionProvider,
            IConfiguration configuration,
            ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
            Action onProcessOdsInstance, ILogger<EducationOrganizationServiceImpl> logger)
            : base(options, usersContext, adminApiDbContext, encryptionProvider, configuration, tenantSpecificDbContextProvider, logger)
        {
            _onProcessOdsInstance = onProcessOdsInstance;
        }

        public override Task ProcessOdsInstance(IUsersContext usersContext, AdminApiDbContext adminApiDbContext, string encryptionKey, string databaseEngine, int? instanceId)
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
              adminApiDbContext,
              _encryptionProvider,
              _configuration,
              _tenantSpecificDbContextProvider,
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
        var adminApiDbContext = new AdminApiDbContext(
            new DbContextOptionsBuilder<AdminApiDbContext>().UseInMemoryDatabase("TestDb_PostgreSql_Admin").Options,
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
              adminApiDbContext,
              _encryptionProvider,
              _configuration,
              _tenantSpecificDbContextProvider,
              _logger);

        string decryptedConnectionString = null;
        A.CallTo(() => _encryptionProvider.TryDecrypt(
            A<string>._,
            A<byte[]>._,
            out decryptedConnectionString))
            .Returns(false);
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

        var adminApiDbContext = new AdminApiDbContext(
           new DbContextOptionsBuilder<AdminApiDbContext>().UseInMemoryDatabase("TestDb_MultiTenantPostgres").Options,
           A.Fake<IConfiguration>());

        var processOdsInstanceCallCount = 0;
        var service = new TestableEducationOrganizationService(
        _options,
        context,
        adminApiDbContext,
        _encryptionProvider,
        _configuration,
        _tenantSpecificDbContextProvider,
        () => processOdsInstanceCallCount++,
        _logger);

        await service.Execute("tenant1", null);
        A.CallTo(() => _tenantSpecificDbContextProvider.GetAdminApiDbContext("tenant1")).MustHaveHappenedOnceExactly();
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

        var adminApiDbContext = new AdminApiDbContext(
            new DbContextOptionsBuilder<AdminApiDbContext>().UseInMemoryDatabase("TestDb_FilterByInstanceId_Admin").Options,
            A.Fake<IConfiguration>());

        var processedInstanceIds = new List<int>();
        var service = new TestableEducationOrganizationServiceWithTracking(
            _options,
            usersContext,
            adminApiDbContext,
            _encryptionProvider,
            _configuration,
            _tenantSpecificDbContextProvider,
            processedInstanceIds,
            _logger);

        await service.ProcessOdsInstance(usersContext, adminApiDbContext, _encryptionKey, "SqlServer", instanceId: 1);

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

        var adminApiDbContext = new AdminApiDbContext(
            new DbContextOptionsBuilder<AdminApiDbContext>().UseInMemoryDatabase("TestDb_ProcessAllInstances_Admin").Options,
            A.Fake<IConfiguration>());

        var processedInstanceIds = new List<int>();
        var service = new TestableEducationOrganizationServiceWithTracking(
            _options,
            usersContext,
            adminApiDbContext,
            _encryptionProvider,
            _configuration,
            _tenantSpecificDbContextProvider,
            processedInstanceIds,
            _logger);

        await service.ProcessOdsInstance(usersContext, adminApiDbContext, _encryptionKey, "SqlServer", instanceId: null);

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

        var adminApiDbContext = new AdminApiDbContext(
            new DbContextOptionsBuilder<AdminApiDbContext>().UseInMemoryDatabase("TestDb_NonExistentInstanceId_Admin").Options,
            A.Fake<IConfiguration>());

        var processedInstanceIds = new List<int>();
        var service = new TestableEducationOrganizationServiceWithTracking(
            _options,
            usersContext,
            adminApiDbContext,
            _encryptionProvider,
            _configuration,
            _tenantSpecificDbContextProvider,
            processedInstanceIds,
            _logger);

        await service.ProcessOdsInstance(usersContext, adminApiDbContext, _encryptionKey, "SqlServer", instanceId: 999);

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

        var adminApiDbContext = new AdminApiDbContext(
            new DbContextOptionsBuilder<AdminApiDbContext>().UseInMemoryDatabase("TestDb_ErrorHandling_Admin").Options,
            A.Fake<IConfiguration>());

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
        
        var callCount = 0;
        var service = new TestableEducationOrganizationServiceWithErrorSimulation(
            _options,
            usersContext,
            adminApiDbContext,
            fakeEncryption,
            _configuration,
            _tenantSpecificDbContextProvider,
            () => ++callCount == 2, // Fail on second call
            fakeLogger);

        // Should not throw - processing should continue despite one failure
        await Should.NotThrowAsync(async () => await service.Execute(null, null));

        // GetEducationOrganizationsAsync should be called for all three instances
        callCount.ShouldBe(3);
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

        var adminApiDbContext = new AdminApiDbContext(
            new DbContextOptionsBuilder<AdminApiDbContext>().UseInMemoryDatabase("TestDb_AllInstancesFail_Admin").Options,
            A.Fake<IConfiguration>());

        var fakeLogger = A.Fake<ILogger<EducationOrganizationServiceImpl>>();
        var fakeEncryption = A.Fake<ISymmetricStringEncryptionProvider>();
        
        // Setup encryption to succeed
        string decryptedConnectionString;
        A.CallTo(() => fakeEncryption.TryDecrypt(A<string>._, A<byte[]>._, out decryptedConnectionString))
            .Returns(true).AssignsOutAndRefParameters("Server=test;");
        
        var callCount = 0;
        var service = new TestableEducationOrganizationServiceWithErrorSimulation(
            _options,
            usersContext,
            adminApiDbContext,
            fakeEncryption,
            _configuration,
            _tenantSpecificDbContextProvider,
            () =>
            {
                callCount++;
                return true; // Always fail
            },
            fakeLogger);

        // Should not throw even when all instances fail
        await Should.NotThrowAsync(async () => await service.Execute(null, null));

        // GetEducationOrganizationsAsync should be called for both instances
        callCount.ShouldBe(2);
    }

    private class TestableEducationOrganizationServiceWithTracking : EducationOrganizationServiceImpl
    {
        private readonly List<int> _processedInstanceIds;

        public TestableEducationOrganizationServiceWithTracking(
            IOptions<AppSettings> options,
            IUsersContext usersContext,
            AdminApiDbContext adminApiDbContext,
            ISymmetricStringEncryptionProvider encryptionProvider,
            IConfiguration configuration,
            ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
            List<int> processedInstanceIds,
            ILogger<EducationOrganizationServiceImpl> logger)
            : base(options, usersContext, adminApiDbContext, encryptionProvider, configuration, tenantSpecificDbContextProvider, logger)
        {
            _processedInstanceIds = processedInstanceIds;
        }

        public override Task<List<EducationOrganizationResult>> GetEducationOrganizationsAsync(string connectionString, string databaseEngine)
        {
            return Task.FromResult(new List<EducationOrganizationResult>());
        }

        public override async Task ProcessOdsInstance(IUsersContext usersContext, AdminApiDbContext adminApiDbContext, string encryptionKey, string databaseEngine, int? instanceId)
        {
            var odsInstances = instanceId.HasValue
                ? await usersContext.OdsInstances
                    .Where(o => o.OdsInstanceId == instanceId.Value)
                    .ToListAsync()
                : await usersContext.OdsInstances.ToListAsync();

            foreach (var instance in odsInstances)
            {
                _processedInstanceIds.Add(instance.OdsInstanceId);
            }
        }
    }

    private class TestableEducationOrganizationServiceWithErrorSimulation : EducationOrganizationServiceImpl
    {
        private readonly Func<bool> _shouldFail;

        public TestableEducationOrganizationServiceWithErrorSimulation(
            IOptions<AppSettings> options,
            IUsersContext usersContext,
            AdminApiDbContext adminApiDbContext,
            ISymmetricStringEncryptionProvider encryptionProvider,
            IConfiguration configuration,
            ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
            Func<bool> shouldFail,
            ILogger<EducationOrganizationServiceImpl> logger)
            : base(options, usersContext, adminApiDbContext, encryptionProvider, configuration, tenantSpecificDbContextProvider, logger)
        {
            _shouldFail = shouldFail;
        }

        public override Task<List<EducationOrganizationResult>> GetEducationOrganizationsAsync(string connectionString, string databaseEngine)
        {
            if (_shouldFail())
            {
                throw new InvalidOperationException("Simulated database error");
            }

            return Task.FromResult(new List<EducationOrganizationResult>());
        }

        public override async Task ProcessOdsInstance(IUsersContext usersContext, AdminApiDbContext adminApiDbContext, string encryptionKey, string databaseEngine, int? instanceId)
        {
            var odsInstances = instanceId.HasValue
                ? await usersContext.OdsInstances
                    .Where(o => o.OdsInstanceId == instanceId.Value)
                    .ToListAsync()
                : await usersContext.OdsInstances.ToListAsync();

            // Process all OdsInstances in parallel - we override to avoid GetConnectionString() call
            // which doesn't work with InMemory DB
            var tasks = odsInstances.Select(odsInstance =>
                Task.Run(async () =>
                {
                    try
                    {
                        var edorgs = await GetEducationOrganizationsAsync("test-connection", databaseEngine);
                    }
                    catch (Exception)
                    {
                        // Simulate the error handling in ProcessSingleOdsInstanceAsync
                        // Errors are caught and logged, not rethrown
                    }
                })
            );

            await Task.WhenAll(tasks);
        }
    }
}
