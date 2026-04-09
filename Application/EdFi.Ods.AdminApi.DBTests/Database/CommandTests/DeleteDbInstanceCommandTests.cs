// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.DBTests.Database.CommandTests;

[TestFixture]
public class DeleteDbInstanceCommandTests : AdminApiDbContextTestBase
{
    [Test]
    public void ShouldSetStatusToPendingDelete()
    {
        var instance = new DbInstance
        {
            Name = "Delete Test Instance",
            Status = DbInstanceStatus.Pending.ToString(),
            DatabaseTemplate = "Minimal",
            LastRefreshed = DateTime.UtcNow
        };
        Save(instance);

        Transaction(context =>
        {
            var command = new DeleteDbInstanceCommand(context);
            command.Execute(instance.Id);
        });

        Transaction(context =>
        {
            var updated = context.DbInstances.Single(d => d.Id == instance.Id);
            updated.Status.ShouldBe(DbInstanceStatus.PendingDelete.ToString());
        });
    }

    [Test]
    public void ShouldUpdateLastModifiedDate()
    {
        var before = DateTime.UtcNow;
        var instance = new DbInstance
        {
            Name = "Timestamp Test Instance",
            Status = DbInstanceStatus.Pending.ToString(),
            DatabaseTemplate = "Minimal",
            LastRefreshed = DateTime.UtcNow
        };
        Save(instance);

        Transaction(context =>
        {
            var command = new DeleteDbInstanceCommand(context);
            command.Execute(instance.Id);
        });

        Transaction(context =>
        {
            var updated = context.DbInstances.Single(d => d.Id == instance.Id);
            updated.LastModifiedDate.ShouldNotBeNull();
            updated.LastModifiedDate!.Value.ShouldBeGreaterThanOrEqualTo(before);
        });
    }

    [Test]
    public void ShouldNotHardDeleteRecord()
    {
        var instance = new DbInstance
        {
            Name = "No Hard Delete Instance",
            Status = DbInstanceStatus.Pending.ToString(),
            DatabaseTemplate = "Minimal",
            LastRefreshed = DateTime.UtcNow
        };
        Save(instance);

        Transaction(context =>
        {
            var command = new DeleteDbInstanceCommand(context);
            command.Execute(instance.Id);
        });

        Transaction(context =>
        {
            var stillExists = context.DbInstances.Any(d => d.Id == instance.Id);
            stillExists.ShouldBeTrue();
        });
    }
}
