// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.DBTests.Database.CommandTests;

[TestFixture]
public class DeleteDataStoreManageCommandTests : AdminApiDbContextTestBase
{
    [Test]
    public void ShouldSetStatusToPendingDelete()
    {
        var instance = new OdsInstanceManage
        {
            Name = "Delete Test Instance",
            Status = OdsInstanceManageStatus.PendingCreate.ToString(),
            DatabaseTemplate = "Minimal",
            LastRefreshed = DateTime.UtcNow
        };
        Save(instance);

        Transaction(context =>
        {
            var command = new DeleteDataStoreManageCommand(context);
            command.Execute(instance.Id);
        });

        Transaction(context =>
        {
            var updated = context.OdsInstanceManages.Single(d => d.Id == instance.Id);
            updated.Status.ShouldBe(OdsInstanceManageStatus.PendingDelete.ToString());
        });
    }

    [Test]
    public void ShouldUpdateLastModifiedDate()
    {
        var before = DateTime.UtcNow;
        var instance = new OdsInstanceManage
        {
            Name = "Timestamp Test Instance",
            Status = OdsInstanceManageStatus.PendingCreate.ToString(),
            DatabaseTemplate = "Minimal",
            LastRefreshed = DateTime.UtcNow
        };
        Save(instance);

        Transaction(context =>
        {
            var command = new DeleteDataStoreManageCommand(context);
            command.Execute(instance.Id);
        });

        Transaction(context =>
        {
            var updated = context.OdsInstanceManages.Single(d => d.Id == instance.Id);
            updated.LastModifiedDate.ShouldNotBeNull();
            updated.LastModifiedDate!.Value.ShouldBeGreaterThanOrEqualTo(before);
        });
    }

    [Test]
    public void ShouldNotHardDeleteRecord()
    {
        var instance = new OdsInstanceManage
        {
            Name = "No Hard Delete Instance",
            Status = OdsInstanceManageStatus.PendingCreate.ToString(),
            DatabaseTemplate = "Minimal",
            LastRefreshed = DateTime.UtcNow
        };
        Save(instance);

        Transaction(context =>
        {
            var command = new DeleteDataStoreManageCommand(context);
            command.Execute(instance.Id);
        });

        Transaction(context =>
        {
            var stillExists = context.OdsInstanceManages.Any(d => d.Id == instance.Id);
            stillExists.ShouldBeTrue();
        });
    }
}



