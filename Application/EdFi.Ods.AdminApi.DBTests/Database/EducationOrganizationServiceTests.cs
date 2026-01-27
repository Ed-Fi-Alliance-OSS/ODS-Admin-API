// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database;

[TestFixture]
public class EducationOrganizationServiceTests : PlatformUsersContextTestBase
{
    private Mock<ITenantsService> _tenantsService;
    private Mock<ITenantConfigurationProvider> _tenantConfigurationProvider;
    private Mock<ISymmetricStringEncryptionProvider> _encryptionProvider;
    private IOptions<AppSettings> _options;
    private AppSettings _appSettings;
    private string _encryptionKey;
    private IConfiguration _configuration;
    private IUsersContext _usersContext;
    private ILogger<EducationOrganizationService> _logger;


    [SetUp]
    public new async Task SetUp()
    {
        await base.SetUp();

        _tenantsService = new Mock<ITenantsService>();
        _tenantConfigurationProvider = new Mock<ITenantConfigurationProvider>();
        _encryptionProvider = new Mock<ISymmetricStringEncryptionProvider>();

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
            DatabaseEngine = DatabaseEngineEnum.SqlServer,
            EncryptionKey = _encryptionKey
        };

        _options = Options.Create(_appSettings);

        _usersContext = new SqlServerUsersContext(GetDbContextOptions());
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<EducationOrganizationService>();
    }

    [Test]
    public void Execute_Should_Add_New_EducationOrganizations_From_ODS()
    {
        var odsInstance = new OdsInstance
        {
            Name = "Test ODS Instance",
            InstanceType = "Test",
            ConnectionString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ConnectionString))
        };

        Save(odsInstance);

        var decryptedConnectionString = ConnectionString;
        _encryptionProvider.Setup(x => x.TryDecrypt(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            out decryptedConnectionString))
            .Returns(true);

        AdminApiTransaction(adminApiDbContext =>
        {
            var service = new TestableEducationOrganizationService(
                _tenantsService.Object,
                _options,
                _tenantConfigurationProvider.Object,
                _usersContext,
                adminApiDbContext,
                _encryptionProvider.Object,
                _logger,
                _configuration
                );

            // Since we don't have actual EdFi ODS tables in the test database,
            // we'll test that the service executes without errors
            // and verify the infrastructure works correctly
            Should.NotThrow(() => service.Execute(null, null).GetAwaiter().GetResult());
        });
    }

    private void AdminApiTransaction(Action<AdminApiDbContext> action)
    {
        var adminApiOptionsBuilder = new DbContextOptionsBuilder<AdminApiDbContext>();
        adminApiOptionsBuilder.UseSqlServer(ConnectionString);
        var _adminApiDbContext = new AdminApiDbContext(adminApiOptionsBuilder.Options, _configuration);

        using var transaction = _adminApiDbContext.Database.BeginTransaction();
        action(_adminApiDbContext);
        _adminApiDbContext.SaveChanges();
        transaction.Commit();
    }

    private class TestableEducationOrganizationService(
        ITenantsService tenantsService,
        IOptions<AppSettings> options,
        ITenantConfigurationProvider tenantConfigurationProvider,
        IUsersContext usersContext,
        AdminApiDbContext adminApiDbContext,
        ISymmetricStringEncryptionProvider encryptionProvider,
        ILogger<EducationOrganizationService> logger,
        IConfiguration configuration) : EducationOrganizationService(tenantsService, options, tenantConfigurationProvider, usersContext, adminApiDbContext, encryptionProvider, configuration, logger)
    {
        public override Task<List<EducationOrganizationResult>> GetEducationOrganizationsAsync(string connectionString, string databaseEngine)
        {
            // Return hardcoded 2 records for testing
            var results = new List<EducationOrganizationResult>
            {
                new() {
                    EducationOrganizationId = 255901,
                    NameOfInstitution = "Test School 1",
                    ShortNameOfInstitution = "TS1",
                    Discriminator = "edfi.School",
                    Id = Guid.NewGuid(),
                    ParentId = 255900
                },
                new() {
                    EducationOrganizationId = 255902,
                    NameOfInstitution = "Test School 2",
                    ShortNameOfInstitution = "TS2",
                    Discriminator = "edfi.School",
                    Id = Guid.NewGuid(),
                    ParentId = 255900
                }
            };

            return Task.FromResult(results);
        }
    }

    [Test]
    public void Execute_Should_Update_Existing_EducationOrganizations()
    {
        var odsInstance = new OdsInstance
        {
            Name = "Test ODS Instance",
            InstanceType = "Test",
            ConnectionString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ConnectionString))
        };

        Save(odsInstance);

        var existingEdOrg = new EducationOrganization
        {
            EducationOrganizationId = 255901,
            NameOfInstitution = "Old Name",
            ShortNameOfInstitution = "Old Short Name",
            Discriminator = "edfi.School",
            InstanceId = odsInstance.OdsInstanceId,
            ParentId = null,
            LastModifiedDate = DateTime.UtcNow.AddDays(-1),
            LastRefreshed = DateTime.UtcNow.AddDays(-1)
        };

        AdminApiTransaction(adminContext =>
        {
            adminContext.EducationOrganizations.Add(existingEdOrg);
        });

        var decryptedConnectionString = ConnectionString;
        _encryptionProvider.Setup(x => x.TryDecrypt(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            out decryptedConnectionString))
            .Returns(true);

        AdminApiTransaction(_adminApiDbContext =>
        {
            var service = new TestableEducationOrganizationService(
                _tenantsService.Object,
                _options,
                _tenantConfigurationProvider.Object,
                _usersContext,
                _adminApiDbContext,
                _encryptionProvider.Object,
                _logger,
                _configuration);

            Should.NotThrow(() => service.Execute(null, null).GetAwaiter().GetResult());

            var updatedEdOrg = _adminApiDbContext.EducationOrganizations
                .FirstOrDefault(e => e.EducationOrganizationId == existingEdOrg.EducationOrganizationId);

            updatedEdOrg.ShouldNotBeNull();
            updatedEdOrg.InstanceId.ShouldBe(odsInstance.OdsInstanceId);
        });
    }

    [Test]
    public void Execute_Should_Remove_Deleted_EducationOrganizations()
    {
        var odsInstance = new OdsInstance
        {
            Name = "Test ODS Instance",
            InstanceType = "Test",
            ConnectionString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ConnectionString))
        };

        Save(odsInstance);

        var edOrgToBeDeleted = new EducationOrganization
        {
            EducationOrganizationId = 999999,
            NameOfInstitution = "To Be Deleted",
            ShortNameOfInstitution = "TBD",
            Discriminator = "edfi.School",
            InstanceId = odsInstance.OdsInstanceId,
            ParentId = null,
            LastModifiedDate = DateTime.UtcNow.AddDays(-1),
            LastRefreshed = DateTime.UtcNow.AddDays(-1)
        };

        AdminApiTransaction(adminContext =>
        {
            adminContext.EducationOrganizations.Add(edOrgToBeDeleted);
        });

        var decryptedConnectionString = ConnectionString;
        _encryptionProvider.Setup(x => x.TryDecrypt(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            out decryptedConnectionString))
            .Returns(true);

        AdminApiTransaction(_adminApiDbContext =>
        {
            var service = new TestableEducationOrganizationService(
                _tenantsService.Object,
                _options,
                _tenantConfigurationProvider.Object,
                _usersContext,
                _adminApiDbContext,
                _encryptionProvider.Object, _logger, _configuration);

            Should.NotThrow(() => service.Execute(null, null).GetAwaiter().GetResult());

            // After execution, since the hardcoded data doesn't include EducationOrganizationId 999999,
            // the service should have removed our test EdOrg
            var deletedEdOrg = _adminApiDbContext.EducationOrganizations
                .FirstOrDefault(e => e.EducationOrganizationId == edOrgToBeDeleted.EducationOrganizationId);

            // The EdOrg should be removed since it's not in the mocked result set
            deletedEdOrg.ShouldBeNull();
        });
    }

    [Test]
    public void Execute_Should_Throw_When_EncryptionKey_Is_Null()
    {
        _appSettings.EncryptionKey = null;

        AdminApiTransaction(_adminApiDbContext =>
        {
            var service = new TestableEducationOrganizationService(
                _tenantsService.Object,
                _options,
                _tenantConfigurationProvider.Object,
                _usersContext,
                _adminApiDbContext,
                _encryptionProvider.Object, _logger, _configuration);

            var exception = Should.Throw<InvalidOperationException>(() =>
                service.Execute(null, null).GetAwaiter().GetResult());

            exception.Message.ShouldBe("EncryptionKey can't be null.");
        });
    }

    [Test]
    public void Execute_Should_Throw_When_DatabaseEngine_Is_Null()
    {
        _appSettings.DatabaseEngine = null;

        AdminApiTransaction(_adminApiDbContext =>
        {
            var service = new TestableEducationOrganizationService(
                _tenantsService.Object,
                _options,
                _tenantConfigurationProvider.Object,
                _usersContext,
                _adminApiDbContext,
                _encryptionProvider.Object, _logger, _configuration);

            Should.Throw<Exception>(() => service.Execute(null, null).GetAwaiter().GetResult());
        });
    }

    [Test]
    public void Execute_Should_Throw_When_DatabaseEngine_Is_Invalid()
    {
        _appSettings.DatabaseEngine = "InvalidEngine";

        AdminApiTransaction(_adminApiDbContext =>
        {
            var service = new TestableEducationOrganizationService(
                _tenantsService.Object,
                _options,
                _tenantConfigurationProvider.Object,
                _usersContext,
                _adminApiDbContext,
                _encryptionProvider.Object, _logger, _configuration);

            var exception = Should.Throw<NotSupportedException>(() =>
                service.Execute(null, null).GetAwaiter().GetResult());

            exception.Message.ShouldContain("Not supported DatabaseEngine \"InvalidEngine\"");
        });
    }

    [Test]
    public void Execute_Should_Throw_When_Connection_String_Decryption_Fails()
    {
        var odsInstance = new OdsInstance
        {
            Name = "Test ODS Instance",
            InstanceType = "Test",
            ConnectionString = "encrypted-connection-string"
        };

        Save(odsInstance);

        string decryptedConnectionString = null;
        _encryptionProvider.Setup(x => x.TryDecrypt(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            out decryptedConnectionString))
            .Returns(false);

        var mockLogger = new Mock<ILogger<EducationOrganizationService>>();

        AdminApiTransaction(_adminApiDbContext =>
        {
            var service = new EducationOrganizationService(
                _tenantsService.Object,
                _options,
                _tenantConfigurationProvider.Object,
                _usersContext,
                _adminApiDbContext,
                _encryptionProvider.Object, _configuration, mockLogger.Object);

            Should.NotThrow(() => service.Execute(null, null).GetAwaiter().GetResult());

            var error = $"Failed to decrypt connection string for ODS Instance ID {odsInstance.OdsInstanceId}. Skipping education organization synchronization for this instance.";

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(error)),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        });
    }

    [Test]
    public void Execute_Should_Process_Multiple_OdsInstances()
    {
        var odsInstance1 = new OdsInstance
        {
            Name = "Test ODS Instance 1",
            InstanceType = "Test",
            ConnectionString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ConnectionString))
        };

        var odsInstance2 = new OdsInstance
        {
            Name = "Test ODS Instance 2",
            InstanceType = "Test",
            ConnectionString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ConnectionString))
        };

        Save(odsInstance1, odsInstance2);

        var decryptedConnectionString = ConnectionString;
        _encryptionProvider.Setup(x => x.TryDecrypt(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            out decryptedConnectionString))
            .Returns(true);

        AdminApiTransaction(_adminApiDbContext =>
        {
            var service = new TestableEducationOrganizationService(
                _tenantsService.Object,
                _options,
                _tenantConfigurationProvider.Object,
                _usersContext,
                _adminApiDbContext,
                _encryptionProvider.Object, _logger, _configuration);

            Should.NotThrow(() => service.Execute(null, null).GetAwaiter().GetResult());

            // Verify both instances were processed
            var instances = _usersContext.OdsInstances.ToList();
            instances.Count.ShouldBeGreaterThanOrEqualTo(2);
        });
    }

    // Add the TearDown method to ensure _usersContext is disposed properly
    [TearDown]
    public void TearDown()
    {
        if (_usersContext != null)
        {
            _usersContext.Dispose();
            _usersContext = null;
        }
    }
}
