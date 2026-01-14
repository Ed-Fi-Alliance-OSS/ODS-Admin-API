// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Tenants;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database;
using EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

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
    }

    [Test]
    public async Task Execute_Should_Throw_InvalidOperationException_When_EncryptionKey_Is_Null()
    {
        _appSettings.EncryptionKey = null;
        var contextOptions = new DbContextOptionsBuilder<AdminConsoleSqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_EncryptionKeyNull")
            .Options;
        var context = new AdminConsoleSqlServerUsersContext(contextOptions);

        var service = new EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService.EducationOrganizationService(
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            context,
            _encryptionProvider);

        await Should.ThrowAsync<InvalidOperationException>(async () => await service.Execute())
            .ContinueWith(t => t.Result.Message.ShouldBe("EncryptionKey can't be null."));
    }

    [Test]
    public async Task Execute_Should_Throw_NotFoundException_When_DatabaseEngine_Is_Null()
    {
        _appSettings.DatabaseEngine = null;
        var contextOptions = new DbContextOptionsBuilder<AdminConsoleSqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_DatabaseEngineNull")
            .Options;
        var context = new AdminConsoleSqlServerUsersContext(contextOptions);

        var service = new EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService.EducationOrganizationService(
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            context,
            _encryptionProvider);

        await Should.ThrowAsync<Exception>(async () => await service.Execute());
    }

    [Test]
    public async Task Execute_Should_Process_Single_Tenant_When_MultiTenancy_Disabled()
    {
        var contextOptions = new DbContextOptionsBuilder<AdminConsoleSqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_SingleTenant")
            .Options;

        using var context = new AdminConsoleSqlServerUsersContext(contextOptions);

        var odsInstance = new OdsInstance
        {
            OdsInstanceId = 1,
            Name = "TestInstance",
            ConnectionString = "encrypted-connection-string"
        };
        context.OdsInstances.Add(odsInstance);
        await context.SaveChangesAsync();

        var service = new EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService.EducationOrganizationService(
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            context,
            _encryptionProvider);

        string decryptedConnectionString = null;
        A.CallTo(() => _encryptionProvider.TryDecrypt(
            A<string>._,
            A<byte[]>._,
            out decryptedConnectionString))
            .Returns(false);

        await Should.ThrowAsync<InvalidOperationException>(async () => await service.Execute())
            .ContinueWith(t => t.Result.Message.ShouldBe("Decrypted connection string can't be null."));
    }

    [Test]
    public async Task Execute_Should_Process_Multiple_Tenants_When_MultiTenancy_Enabled()
    {
        _appSettings.MultiTenancy = true;

        var tenants = new List<TenantModel>
        {
            new TenantModel
            {
                TenantName = "tenant1",
                ConnectionStrings = new TenantModelConnectionStrings
                {
                    EdFiAdminConnectionString = "Data Source=.\\;Initial Catalog=EdFi_AdminTenant1;Integrated Security=True;Trusted_Connection=true;Encrypt=True;TrustServerCertificate=True",
                    EdFiSecurityConnectionString = "Server=localhost;Database=EdFi_Security_Tenant1;TrustServerCertificate=True"
                }
            },
            new TenantModel
            {
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

        var contextOptions = new DbContextOptionsBuilder<AdminConsoleSqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_MultiTenant")
            .Options;

        using var context = new AdminConsoleSqlServerUsersContext(contextOptions);

        var processOdsInstanceCallCount = 0;
        var service = new TestableEducationOrganizationService(
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            context,
            _encryptionProvider,
            () => processOdsInstanceCallCount++);

        await service.Execute();

        A.CallTo(() => _tenantsService.GetTenantsAsync(false)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _tenantConfigurationProvider.Get()).MustHaveHappened();
        processOdsInstanceCallCount.ShouldBe(2);
    }

    private class TestableEducationOrganizationService : EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService.EducationOrganizationService
    {
        private readonly Action _onProcessOdsInstance;

        public TestableEducationOrganizationService(
            ITenantsService tenantsService,
            IOptions<AppSettings> options,
            ITenantConfigurationProvider tenantConfigurationProvider,
            IAdminApiUserContext adminApiUsersContext,
            ISymmetricStringEncryptionProvider encryptionProvider,
            Action onProcessOdsInstance)
            : base(tenantsService, options, tenantConfigurationProvider, adminApiUsersContext, encryptionProvider)
        {
            _onProcessOdsInstance = onProcessOdsInstance;
        }

        public override Task ProcessOdsInstance(IAdminApiUserContext context, string encryptionKey, string databaseEngine)
        {
            _onProcessOdsInstance();
            return Task.CompletedTask;
        }
    }

    [Test]
    public async Task Execute_Should_Throw_NotSupportedException_When_DatabaseEngine_Is_Invalid()
    {
        _appSettings.DatabaseEngine = "InvalidEngine";

        var contextOptions = new DbContextOptionsBuilder<AdminConsoleSqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_InvalidEngine")
            .Options;

        using var context = new AdminConsoleSqlServerUsersContext(contextOptions);

        var service = new EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService.EducationOrganizationService(
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            context,
            _encryptionProvider);

        var exception = await Should.ThrowAsync<NotSupportedException>(async () => await service.Execute());
        exception.Message.ShouldContain("Not supported DatabaseEngine \"InvalidEngine\"");
    }

    [Test]
    public async Task Execute_Should_Handle_PostgreSql_DatabaseEngine()
    {
        _appSettings.DatabaseEngine = "PostgreSql";

        var contextOptions = new DbContextOptionsBuilder<AdminConsolePostgresUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_PostgreSql")
            .Options;

        using var context = new AdminConsolePostgresUsersContext(contextOptions);

        var odsInstance = new OdsInstance
        {
            OdsInstanceId = 1,
            Name = "TestInstance",
            ConnectionString = "encrypted-connection-string"
        };
        context.OdsInstances.Add(odsInstance);
        await context.SaveChangesAsync();

        var service = new EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService.EducationOrganizationService(
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            context,
            _encryptionProvider);

        string decryptedConnectionString = null;
        A.CallTo(() => _encryptionProvider.TryDecrypt(
            A<string>._,
            A<byte[]>._,
            out decryptedConnectionString))
            .Returns(false);

        await Should.ThrowAsync<InvalidOperationException>(async () => await service.Execute());
    }

    [Test]
    public async Task Execute_Should_Process_MultiTenancy_With_PostgreSql()
    {
        _appSettings.MultiTenancy = true;
        _appSettings.DatabaseEngine = "PostgreSql";

        var tenants = new List<TenantModel>
        {
            new TenantModel
            {
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

        var contextOptions = new DbContextOptionsBuilder<AdminConsolePostgresUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_MultiTenantPostgres")
            .Options;

        using var context = new AdminConsolePostgresUsersContext(contextOptions);

        var processOdsInstanceCallCount = 0;
        var service = new TestableEducationOrganizationService(
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            context,
            _encryptionProvider,
            () => processOdsInstanceCallCount++);

        await service.Execute();

        A.CallTo(() => _tenantsService.GetTenantsAsync(false)).MustHaveHappenedOnceExactly();
        processOdsInstanceCallCount.ShouldBe(1);
    }

    [Test]
    public async Task Execute_Should_Handle_Empty_Tenant_List()
    {
        _appSettings.MultiTenancy = true;

        A.CallTo(() => _tenantsService.GetTenantsAsync(false)).Returns(new List<TenantModel>());

        var contextOptions = new DbContextOptionsBuilder<AdminConsoleSqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_EmptyTenants")
            .Options;

        using var context = new AdminConsoleSqlServerUsersContext(contextOptions);

        var service = new EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService.EducationOrganizationService(
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            context,
            _encryptionProvider);

        await service.Execute();

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

        var contextOptions = new DbContextOptionsBuilder<AdminConsoleSqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_NoTenantConfig")
            .Options;

        using var context = new AdminConsoleSqlServerUsersContext(contextOptions);

        var service = new EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService.EducationOrganizationService(
            _tenantsService,
            _options,
            _tenantConfigurationProvider,
            context,
            _encryptionProvider);

        await service.Execute();

        A.CallTo(() => _tenantsService.GetTenantsAsync(false)).MustHaveHappenedOnceExactly();
    }
}
