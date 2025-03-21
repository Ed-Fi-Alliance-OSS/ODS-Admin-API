// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts.Admin.MsSql;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminConsole.DBTests.Database.CommandTests;

[TestFixture]
public class EditInstanceCommandTests : PlatformUsersContextTestBase
{
    private int _odsInstanceId;

    [SetUp]
    public void Init()
    {
        var originalOdsInstance = new Instance
        {
            TenantId = 1,
            TenantName = "Test Tenant",
            OdsInstanceId = 1,
            InstanceName = "Test Instance",
            InstanceType = "Standard",
            Status = InstanceStatus.Completed,
            OdsInstanceContexts = new List<AdminApi.AdminConsole.Infrastructure.DataAccess.Models.OdsInstanceContext>(),
            OdsInstanceDerivatives = new List<AdminApi.AdminConsole.Infrastructure.DataAccess.Models.OdsInstanceDerivative>()
        };
        Save(originalOdsInstance);
        _odsInstanceId = originalOdsInstance.Id;
    }

    [Test]
    public void ShouldEditInstance()
    {
        AdminConsoleSqlServerUsersContext userDbContext = new(GetUserDbContextOptions());
        var newInstanceData = new Mock<IInstanceRequestModel>();
        newInstanceData.Setup(v => v.OdsInstanceId).Returns(1);
        newInstanceData.Setup(v => v.TenantId).Returns(1);
        newInstanceData.Setup(v => v.TenantName).Returns("UpdateTenantName");
        newInstanceData.Setup(v => v.Name).Returns("Updated Instance");
        newInstanceData.Setup(v => v.InstanceType).Returns("Updated Type");
        newInstanceData.Setup(v => v.OdsInstanceContexts).Returns(new List<OdsInstanceContextModel>());
        newInstanceData.Setup(v => v.OdsInstanceDerivatives).Returns(new List<OdsInstanceDerivativeModel>());

        Transaction(async dbContext =>
        {
            var repository = new CommandRepository<Instance>(dbContext);
            var qRepository = new QueriesRepository<Instance>(dbContext);
            var command = new EditInstanceCommand(repository, qRepository, dbContext, userDbContext);

            var result = await command.Execute(_odsInstanceId, newInstanceData.Object);
        });

        Transaction(dbContext =>
        {
            var persistedInstance = dbContext.Instances;
            persistedInstance.Count().ShouldBe(1);
            var instance = persistedInstance.First();
            instance.TenantId.ShouldBe(1);
            instance.TenantName.ShouldBe("UpdateTenantName");
            instance.OdsInstanceId.ShouldBe(1);
            instance.InstanceName.ShouldBe("Updated Instance");
            instance.InstanceType.ShouldBe("Updated Type");
            instance.OdsInstanceContexts.ShouldBeEmpty();
            instance.OdsInstanceDerivatives.ShouldBeEmpty();
        });
    }

    [Test]
    public void ShouldSetPendingRenameStatusWhenNameChanges()
    {
        AdminConsoleSqlServerUsersContext userDbContext = new(GetUserDbContextOptions());
        var newInstanceData = new Mock<IInstanceRequestModel>();
        newInstanceData.Setup(v => v.OdsInstanceId).Returns(1);
        newInstanceData.Setup(v => v.TenantId).Returns(1);
        newInstanceData.Setup(v => v.TenantName).Returns("UpdateTenantName");
        newInstanceData.Setup(v => v.Name).Returns("Updated Instance");
        newInstanceData.Setup(v => v.InstanceType).Returns("Updated Type");
        newInstanceData.Setup(v => v.OdsInstanceContexts).Returns(new List<OdsInstanceContextModel>());
        newInstanceData.Setup(v => v.OdsInstanceDerivatives).Returns(new List<OdsInstanceDerivativeModel>());

        Transaction(async dbContext =>
        {
            var repository = new CommandRepository<Instance>(dbContext);
            var qRepository = new QueriesRepository<Instance>(dbContext);
            var command = new EditInstanceCommand(repository, qRepository, dbContext, userDbContext);
            var result = await command.Execute(_odsInstanceId, newInstanceData.Object);
        });

        Transaction(dbContext =>
        {
            var persistedInstance = dbContext.Instances;
            persistedInstance.Count().ShouldBe(1);
            var instance = persistedInstance.First();
            instance.TenantId.ShouldBe(1);
            instance.TenantName.ShouldBe("UpdateTenantName");
            instance.OdsInstanceId.ShouldBe(1);
            instance.InstanceName.ShouldBe("Updated Instance");
            instance.InstanceType.ShouldBe("Updated Type");
            instance.Status.ToString().ShouldBe(InstanceStatus.Pending_Rename.ToString());
            instance.OdsInstanceContexts.ShouldBeEmpty();
            instance.OdsInstanceDerivatives.ShouldBeEmpty();
        });
    }
}
