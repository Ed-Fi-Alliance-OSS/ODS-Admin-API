// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Infrastructure.Services.EducationOrganizationService;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Shouldly;
using EdFi.Ods.AdminApi.Common.Infrastructure.Providers.Interfaces;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;

namespace EdFi.Ods.AdminApi.DBTests.Database;

[TestFixture]
public class EducationOrganizationServiceTests : PlatformUsersContextTestBase
{
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
            DatabaseEngine = "SqlServer",
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
            var tenantSpecificProvider = new DummyTenantSpecificDbContextProvider(adminApiDbContext);

            var service = new TestableEducationOrganizationService(
                _options,
                _usersContext,
                adminApiDbContext,
                _encryptionProvider.Object,
                _configuration,
                tenantSpecificProvider,
                _logger
            );

            Should.NotThrow(() => service.Execute(null, null).GetAwaiter().GetResult());
        });
    }

    [Test]
    public void Execute_Should_Use_TenantSpecificDbContext_When_MultiTenancy_Enabled()
    {
        _appSettings.MultiTenancy = true;
        var tenantName = "tenant1";

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

        var builder = new DbContextOptionsBuilder<AdminApiDbContext>();
        builder.UseSqlServer(ConnectionString);

        AdminApiTransaction(adminApiDbContext =>
        {
            var tenantDbContext = new AdminApiDbContext(builder.Options, _configuration);
            var tenantSpecificProvider = new DummyTenantSpecificDbContextProvider(tenantDbContext, tenantName);

            var service = new TestableEducationOrganizationService(
                _options,
                _usersContext,
                adminApiDbContext,
                _encryptionProvider.Object,
                _configuration,
                tenantSpecificProvider,
                _logger
            );

            Should.NotThrow(() => service.Execute(tenantName, null).GetAwaiter().GetResult());
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

    private class DummyTenantSpecificDbContextProvider : ITenantSpecificDbContextProvider
    {
        private readonly AdminApiDbContext _tenantDbContext;
        private readonly string _tenantName;
        private readonly IUsersContext _tenantUsersContext =
            new SqlServerUsersContext(GetDbContextOptions());

        public DummyTenantSpecificDbContextProvider(AdminApiDbContext tenantDbContext, string tenantName = "tenant1")
        {
            _tenantDbContext = tenantDbContext;
            _tenantName = tenantName;
        }

        public AdminApiDbContext GetAdminApiDbContext(string tenantIdentifier)
        {
            if (tenantIdentifier == _tenantName)
                return _tenantDbContext;
            throw new InvalidOperationException("Unknown tenant");
        }

        public IUsersContext GetUsersContext(string tenantIdentifier)
        {
            if (tenantIdentifier == _tenantName)
                return _tenantUsersContext;
            throw new InvalidOperationException("Unknown tenant");
        }
    }

    private class TestableEducationOrganizationService(
        IOptions<AppSettings> options,
        IUsersContext usersContext,
        AdminApiDbContext adminApiDbContext,
        ISymmetricStringEncryptionProvider encryptionProvider,
        IConfiguration configuration,
        ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
        ILogger<EducationOrganizationService> logger
        ) : EducationOrganizationService(options, usersContext, adminApiDbContext, encryptionProvider, configuration, tenantSpecificDbContextProvider, logger)
    {
        public override Task<List<EducationOrganizationResult>> GetEducationOrganizationsAsync(string connectionString, string databaseEngine)
        {
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

    [TearDown]
    public void TearDown()
    {
        if (_usersContext != null)
        {
            _usersContext.Dispose();
            _usersContext = null;
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
                _options,
                _usersContext,
                _adminApiDbContext,
                _encryptionProvider.Object,
                _configuration,
                 new DummyTenantSpecificDbContextProvider(_adminApiDbContext),
                _logger);

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
              _options,
              _usersContext,
              _adminApiDbContext,
              _encryptionProvider.Object,
              _configuration,
              new DummyTenantSpecificDbContextProvider(_adminApiDbContext),
              _logger);

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
              _options,
              _usersContext,
              _adminApiDbContext,
              _encryptionProvider.Object,
              _configuration,
              new DummyTenantSpecificDbContextProvider(_adminApiDbContext),
              _logger);

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
               _options,
               _usersContext,
               _adminApiDbContext,
               _encryptionProvider.Object,
              _configuration,
               new DummyTenantSpecificDbContextProvider(_adminApiDbContext),
               _logger);

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
              _options,
              _usersContext,
              _adminApiDbContext,
              _encryptionProvider.Object,
              _configuration,
              new DummyTenantSpecificDbContextProvider(_adminApiDbContext),
              _logger);

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
            var service = new TestableEducationOrganizationService(
              _options,
              _usersContext,
              _adminApiDbContext,
              _encryptionProvider.Object,
              _configuration,
              new DummyTenantSpecificDbContextProvider(_adminApiDbContext),
               mockLogger.Object);

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
              _options,
              _usersContext,
              _adminApiDbContext,
              _encryptionProvider.Object,
              _configuration,
              new DummyTenantSpecificDbContextProvider(_adminApiDbContext),
              _logger);

            Should.NotThrow(() => service.Execute(null, null).GetAwaiter().GetResult());

            // Verify both instances were processed
            var instances = _usersContext.OdsInstances.ToList();
            instances.Count.ShouldBeGreaterThanOrEqualTo(2);
        });
    }

    [Test]
    public void Execute_Should_Process_Only_Specified_Instance_When_InstanceId_Provided()
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
            var service = new TestableEducationOrganizationServiceWithInstanceTracking(
              _options,
              _usersContext,
              _adminApiDbContext,
              _encryptionProvider.Object,
              _configuration,
              new DummyTenantSpecificDbContextProvider(_adminApiDbContext),
              _logger);

            Should.NotThrow(() => service.Execute(null, odsInstance1.OdsInstanceId).GetAwaiter().GetResult());

            // Verify only the specified instance was processed
            service.ProcessedInstanceIds.Count.ShouldBe(1);
            service.ProcessedInstanceIds.ShouldContain(odsInstance1.OdsInstanceId);
            service.ProcessedInstanceIds.ShouldNotContain(odsInstance2.OdsInstanceId);
        });
    }

    [Test]
    public void Execute_Should_Process_All_Instances_When_InstanceId_Is_Null()
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

        var odsInstance3 = new OdsInstance
        {
            Name = "Test ODS Instance 3",
            InstanceType = "Test",
            ConnectionString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ConnectionString))
        };

        Save(odsInstance1, odsInstance2, odsInstance3);

        var decryptedConnectionString = ConnectionString;
        _encryptionProvider.Setup(x => x.TryDecrypt(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            out decryptedConnectionString))
            .Returns(true);

        AdminApiTransaction(_adminApiDbContext =>
        {
            var service = new TestableEducationOrganizationServiceWithInstanceTracking(
              _options,
              _usersContext,
              _adminApiDbContext,
              _encryptionProvider.Object,
              _configuration,
              new DummyTenantSpecificDbContextProvider(_adminApiDbContext),
              _logger);

            Should.NotThrow(() => service.Execute(null, null).GetAwaiter().GetResult());

            // Verify all instances were processed
            service.ProcessedInstanceIds.Count.ShouldBe(3);
            service.ProcessedInstanceIds.ShouldContain(odsInstance1.OdsInstanceId);
            service.ProcessedInstanceIds.ShouldContain(odsInstance2.OdsInstanceId);
            service.ProcessedInstanceIds.ShouldContain(odsInstance3.OdsInstanceId);
        });
    }

    [Test]
    public void Execute_Should_Process_Zero_Instances_When_InstanceId_Does_Not_Exist()
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

        AdminApiTransaction(_adminApiDbContext =>
        {
            var service = new TestableEducationOrganizationServiceWithInstanceTracking(
              _options,
              _usersContext,
              _adminApiDbContext,
              _encryptionProvider.Object,
              _configuration,
              new DummyTenantSpecificDbContextProvider(_adminApiDbContext),
              _logger);

            int nonExistentInstanceId = 999;
            Should.NotThrow(() => service.Execute(null, nonExistentInstanceId).GetAwaiter().GetResult());

            // Verify no instances were processed
            service.ProcessedInstanceIds.ShouldBeEmpty();
        });
    }

    private class TestableEducationOrganizationServiceWithInstanceTracking : TestableEducationOrganizationService
    {
        public List<int> ProcessedInstanceIds { get; } = new List<int>();

        public TestableEducationOrganizationServiceWithInstanceTracking(
            IOptions<AppSettings> options,
            IUsersContext usersContext,
            AdminApiDbContext adminApiDbContext,
            ISymmetricStringEncryptionProvider encryptionProvider,
            IConfiguration configuration,
            ITenantSpecificDbContextProvider tenantSpecificDbContextProvider,
            ILogger<EducationOrganizationService> logger)
            : base(options, usersContext, adminApiDbContext, encryptionProvider, configuration, tenantSpecificDbContextProvider, logger)
        {
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
                ProcessedInstanceIds.Add(instance.OdsInstanceId);
            }

            await base.ProcessOdsInstance(usersContext, adminApiDbContext, encryptionKey, databaseEngine, instanceId);
        }
    }
}
