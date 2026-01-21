// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Tenants;
using EdFi.Ods.AdminApi.Infrastructure;
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
    private ITenantsService _tenantsService = null!;
    private IOptions<AppSettings> _options = null!;
    private ITenantConfigurationProvider _tenantConfigurationProvider = null!;
    private ISymmetricStringEncryptionProvider _encryptionProvider = null!;
    private AppSettings _appSettings = null!;
    private string _encryptionKey = null!;
    private ILogger<EducationOrganizationServiceImpl> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _tenantsService = A.Fake<ITenantsService>();
        _options = A.Fake<IOptions<AppSettings>>();
        _tenantConfigurationProvider = A.Fake<ITenantConfigurationProvider>();
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
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            usersContext,
            adminApiDbContext,
            _encryptionProvider,
            A.Fake<IConfiguration>(), _logger);

        await Should.ThrowAsync<InvalidOperationException>(async () => await service.Execute(null))
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
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            usersContext,
            adminApiDbContext,
            _encryptionProvider,
            A.Fake<IConfiguration>(), _logger);

        await Should.ThrowAsync<Exception>(async () => await service.Execute(null));
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
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            usersContext,
            adminApiDbContext,
            _encryptionProvider,
            A.Fake<IConfiguration>(), _logger);

        string decryptedConnectionString = null;
        A.CallTo(() => _encryptionProvider.TryDecrypt(
            A<string>._,
            A<byte[]>._,
            out decryptedConnectionString))
            .Returns(false);

        Should.NotThrow(() => service.Execute(null).GetAwaiter().GetResult());
    }

    [Test]
    public async Task Execute_Should_Process_For_Selected_Tenant_When_MultiTenancy_Enabled()
    {
        _appSettings.MultiTenancy = true;

        var tenants = new List<TenantModel>
        {
            new() {
                TenantName = "tenant1",
                ConnectionStrings = new TenantModelConnectionStrings
                {
                    EdFiAdminConnectionString = "Data Source=.\\;Initial Catalog=EdFi_AdminTenant1;Integrated Security=True;Trusted_Connection=true;Encrypt=True;TrustServerCertificate=True",
                    EdFiSecurityConnectionString = "Server=localhost;Database=EdFi_Security_Tenant1;TrustServerCertificate=True"
                }
            },
            new() {
                TenantName = "tenant2",
                ConnectionStrings = new TenantModelConnectionStrings
                {
                    EdFiAdminConnectionString = "Data Source=.\\;Initial Catalog=EdFi_AdminTenant2;Integrated Security=True;Trusted_Connection=true;Encrypt=True;TrustServerCertificate=True",
                    EdFiSecurityConnectionString = "Server=localhost;Database=EdFi_Security_Tenant2;TrustServerCertificate=True"
                }
            }
        };

        A.CallTo(() => _tenantsService.GetTenantsAsync(false)).Returns(tenants);

        var tenantConfigurations = new Dictionary<string, TenantConfiguration>
        {
            {
                "tenant1", new TenantConfiguration
                {
                    TenantIdentifier = "tenant1",
                    AdminConnectionString = "Data Source=.\\;Initial Catalog=EdFi_AdminTenant1;Integrated Security=True;Trusted_Connection=true;Encrypt=True;TrustServerCertificate=True",
                    SecurityConnectionString = "Server=localhost;Database=EdFi_Security_Tenant1;TrustServerCertificate=True"
                }
            },
            {
                "tenant2", new TenantConfiguration
                {
                    TenantIdentifier = "tenant2",
                    AdminConnectionString = "Data Source=.\\;Initial Catalog=EdFi_AdminTenant2;Integrated Security=True;Trusted_Connection=true;Encrypt=True;TrustServerCertificate=True",
                    SecurityConnectionString = "Server=localhost;Database=EdFi_Security_Tenant2;TrustServerCertificate=True"
                }
            }
        };

        A.CallTo(() => _tenantConfigurationProvider.Get()).Returns(tenantConfigurations);

        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_MultiTenant")
            .Options;

        using var usersContext = new SqlServerUsersContext(contextOptions);
        var adminApiDbContext = new AdminApiDbContext(
            new DbContextOptionsBuilder<AdminApiDbContext>().UseInMemoryDatabase("TestDb_MultiTenant_Admin").Options,
            A.Fake<IConfiguration>());

        var processOdsInstanceCallCount = 0;
        var service = new TestableEducationOrganizationService(
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            usersContext,
            adminApiDbContext,
            _encryptionProvider,
            A.Fake<IConfiguration>(),
            () => processOdsInstanceCallCount++,
            _logger);

        await service.Execute("tenant1");

        A.CallTo(() => _tenantsService.GetTenantsAsync(false)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _tenantConfigurationProvider.Get()).MustHaveHappened();
        processOdsInstanceCallCount.ShouldBe(1);
    }

    private class TestableEducationOrganizationService : EducationOrganizationServiceImpl
    {
        private readonly Action _onProcessOdsInstance;

        public TestableEducationOrganizationService(
            ITenantsService tenantsService,
            IOptions<AppSettings> options,
            ITenantConfigurationProvider tenantConfigurationProvider,
            IUsersContext usersContext,
            AdminApiDbContext adminApiDbContext,
            ISymmetricStringEncryptionProvider encryptionProvider,
            IConfiguration configuration,
            Action onProcessOdsInstance, ILogger<EducationOrganizationServiceImpl> logger)
            : base(tenantsService, options, tenantConfigurationProvider, usersContext, adminApiDbContext, encryptionProvider, configuration, logger)
        {
            _onProcessOdsInstance = onProcessOdsInstance;
        }

        public override Task ProcessOdsInstance(IUsersContext usersContext, AdminApiDbContext adminApiDbContext, string encryptionKey, string databaseEngine)
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
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            usersContext,
            adminApiDbContext,
            _encryptionProvider,
            A.Fake<IConfiguration>(), _logger);

        var exception = await Should.ThrowAsync<NotSupportedException>(async () => await service.Execute(null));
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
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            usersContext,
            adminApiDbContext,
            _encryptionProvider,
            A.Fake<IConfiguration>(), _logger);

        string decryptedConnectionString = null;
        A.CallTo(() => _encryptionProvider.TryDecrypt(
            A<string>._,
            A<byte[]>._,
            out decryptedConnectionString))
            .Returns(false);

        Should.NotThrow(() => service.Execute(null).GetAwaiter().GetResult());
    }

    [Test]
    public async Task Execute_Should_Process_MultiTenancy_With_PostgreSql()
    {
        _appSettings.MultiTenancy = true;
        _appSettings.DatabaseEngine = "PostgreSql";

        var tenants = new List<TenantModel>
        {
            new() {
                TenantName = "tenant1",
                ConnectionStrings = new TenantModelConnectionStrings
                {
                    EdFiAdminConnectionString = "Host=localhost;Database=EdFi_Admin_Tenant1;",
                    EdFiSecurityConnectionString = "Host=localhost;Database=EdFi_Security_Tenant1;"
                }
            }
        };

        A.CallTo(() => _tenantsService.GetTenantsAsync(false)).Returns(tenants);

        var tenantConfigurations = new Dictionary<string, TenantConfiguration>
        {
            {
                "tenant1", new TenantConfiguration
                {
                    TenantIdentifier = "tenant1",
                    AdminConnectionString = "Host=localhost;Database=EdFi_Admin_Tenant1;",
                    SecurityConnectionString = "Host=localhost;Database=EdFi_Security_Tenant1;"
                }
            }
        };

        A.CallTo(() => _tenantConfigurationProvider.Get()).Returns(tenantConfigurations);

        var contextOptions = new DbContextOptionsBuilder<PostgresUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_MultiTenantPostgres")
            .Options;

        using var context = new PostgresUsersContext(contextOptions);

        var adminApiDbContext = new AdminApiDbContext(
           new DbContextOptionsBuilder<AdminApiDbContext>().UseInMemoryDatabase("TestDb_MultiTenantPostgres").Options,
           A.Fake<IConfiguration>());

        var processOdsInstanceCallCount = 0;
        var service = new TestableEducationOrganizationService(
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            context,
            adminApiDbContext,
            _encryptionProvider,
            A.Fake<IConfiguration>(),
            () => processOdsInstanceCallCount++, _logger);

        await service.Execute("tenant1");

        A.CallTo(() => _tenantsService.GetTenantsAsync(false)).MustHaveHappenedOnceExactly();
        processOdsInstanceCallCount.ShouldBe(1);
    }

    [Test]
    public async Task Execute_Should_Handle_Empty_Tenant_List()
    {
        _appSettings.MultiTenancy = true;

        A.CallTo(() => _tenantsService.GetTenantsAsync(false)).Returns(new List<TenantModel>());

        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_EmptyTenants")
            .Options;

        using var usersContext = new SqlServerUsersContext(contextOptions);
        var adminApiDbContext = new AdminApiDbContext(
            new DbContextOptionsBuilder<AdminApiDbContext>().UseInMemoryDatabase("TestDb_EmptyTenants_Admin").Options,
            A.Fake<IConfiguration>());

        var service = new EducationOrganizationServiceImpl(
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            usersContext,
            adminApiDbContext,
            _encryptionProvider,
            A.Fake<IConfiguration>(), _logger);

        await service.Execute(null);

        A.CallTo(() => _tenantsService.GetTenantsAsync(false)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Execute_Should_Skip_Tenant_When_Configuration_Not_Found()
    {
        _appSettings.MultiTenancy = true;

        var tenants = new List<TenantModel>
        {
            new TenantModel
            {
                TenantName = "tenant1",
                ConnectionStrings = new TenantModelConnectionStrings
                {
                    EdFiAdminConnectionString = "Server=localhost;Database=EdFi_Admin_Tenant1;",
                    EdFiSecurityConnectionString = "Server=localhost;Database=EdFi_Security_Tenant1;"
                }
            }
        };

        A.CallTo(() => _tenantsService.GetTenantsAsync(false)).Returns(tenants);

        var emptyTenantConfigurations = new Dictionary<string, TenantConfiguration>();
        A.CallTo(() => _tenantConfigurationProvider.Get()).Returns(emptyTenantConfigurations);

        var contextOptions = new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_NoTenantConfig")
            .Options;

        using var usersContext = new SqlServerUsersContext(contextOptions);
        var adminApiDbContext = new AdminApiDbContext(
            new DbContextOptionsBuilder<AdminApiDbContext>().UseInMemoryDatabase("TestDb_NoTenantConfig_Admin").Options,
            A.Fake<IConfiguration>());

        var service = new EducationOrganizationServiceImpl(
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            usersContext,
            adminApiDbContext,
            _encryptionProvider,
            A.Fake<IConfiguration>(), _logger);

        await service.Execute(null);

        A.CallTo(() => _tenantsService.GetTenantsAsync(false)).MustHaveHappenedOnceExactly();
    }
}
