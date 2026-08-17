// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Ods.AdminApi.DBTestsShared;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.DBTests.Infrastructure.Audit;

[TestFixture]
public class AdminApiAuditLogWriterTests : AdminApiDbContextTestBase
{
    protected override string AdminConnectionString => Testing.AdminConnectionString;

    protected override IConfiguration Configuration => Testing.Configuration();

    [Test]
    public async Task WriteAsync_PersistsAuditLogRowWithAllFields()
    {
        var writer = new AdminApiAuditLogWriter(Testing.Configuration());
        var auditEvent = new AuditEvent
        {
            AdminConnectionString = ConnectionString,
            EventType = AuditEventType.Action,
            Timestamp = new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc),
            ClientId = "test-client",
            SourceIpAddress = "192.168.1.1",
            HttpVerb = "DELETE",
            HttpUrl = "/v3/apiClients/42",
            StatusCode = 204
        };

        await writer.WriteAsync(auditEvent, CancellationToken.None);

        var savedRow = Transaction(context => context.AuditLogs.Single());
        savedRow.EventType.ShouldBe(AuditEventType.Action);
        savedRow.ClientId.ShouldBe("test-client");
        savedRow.SourceIpAddress.ShouldBe("192.168.1.1");
        savedRow.HttpVerb.ShouldBe("DELETE");
        savedRow.HttpUrl.ShouldBe("/v3/apiClients/42");
        savedRow.StatusCode.ShouldBe(204);
    }
}
