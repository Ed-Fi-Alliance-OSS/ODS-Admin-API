// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.DBTestsShared;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.DBTests.Database.CommandTests;

[TestFixture]
public class AddDataStoreManageCommandTests : AdminApiDbContextTestBase
{
    protected override string AdminConnectionString => Testing.AdminConnectionString;

    protected override IConfiguration Configuration => Testing.Configuration();

    [Test]
    public void ShouldAddDataStoreManage()
    {
        var model = new Mock<IAddDataStoreManageModel>();
        model.Setup(x => x.Name).Returns("Test Instance");
        model.Setup(x => x.DatabaseTemplate).Returns("Minimal");

        var id = 0;
        Transaction(context =>
        {
            var command = new AddDataStoreManageCommand(context);
            id = command.Execute(model.Object).Id;
            id.ShouldBeGreaterThan(0);
        });

        Transaction(context =>
        {
            var instance = context.OdsInstanceManages.Single(d => d.Id == id);
            instance.Name.ShouldBe("Test Instance");
            instance.DatabaseTemplate.ShouldBe("Minimal");
            instance.Status.ShouldBe(OdsInstanceManageStatus.PendingCreate.ToString());
            instance.OdsInstanceId.ShouldBeNull();
            instance.OdsInstanceName.ShouldBeNull();
            instance.DatabaseName.ShouldBeNull();
        });
    }

    [Test]
    public void ShouldAddDataStoreManageWithSampleTemplate()
    {
        var model = new Mock<IAddDataStoreManageModel>();
        model.Setup(x => x.Name).Returns("Sample Instance");
        model.Setup(x => x.DatabaseTemplate).Returns("Sample");

        var id = 0;
        Transaction(context =>
        {
            var command = new AddDataStoreManageCommand(context);
            id = command.Execute(model.Object).Id;
            id.ShouldBeGreaterThan(0);
        });

        Transaction(context =>
        {
            var instance = context.OdsInstanceManages.Single(d => d.Id == id);
            instance.DatabaseTemplate.ShouldBe("Sample");
        });
    }

    [Test]
    public void ShouldTrimNameAndDatabaseTemplate()
    {
        var model = new Mock<IAddDataStoreManageModel>();
        model.Setup(x => x.Name).Returns("  Trimmed Instance  ");
        model.Setup(x => x.DatabaseTemplate).Returns("  Minimal  ");

        var id = 0;
        Transaction(context =>
        {
            var command = new AddDataStoreManageCommand(context);
            id = command.Execute(model.Object).Id;
        });

        Transaction(context =>
        {
            var instance = context.OdsInstanceManages.Single(d => d.Id == id);
            instance.Name.ShouldBe("Trimmed Instance");
            instance.DatabaseTemplate.ShouldBe("Minimal");
        });
    }

    [Test]
    public void ShouldSetLastRefreshedAndLastModifiedDate()
    {
        var model = new Mock<IAddDataStoreManageModel>();
        model.Setup(x => x.Name).Returns("Timestamp Instance");
        model.Setup(x => x.DatabaseTemplate).Returns("Minimal");

        var before = DateTime.UtcNow;
        var id = 0;
        Transaction(context =>
        {
            var command = new AddDataStoreManageCommand(context);
            id = command.Execute(model.Object).Id;
        });

        Transaction(context =>
        {
            var instance = context.OdsInstanceManages.Single(d => d.Id == id);
            instance.LastRefreshed.ShouldBeGreaterThanOrEqualTo(before);
            instance.LastModifiedDate.ShouldNotBeNull();
            instance.LastModifiedDate!.Value.ShouldBeGreaterThanOrEqualTo(before);
        });
    }
}



