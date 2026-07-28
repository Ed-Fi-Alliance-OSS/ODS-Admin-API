// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Ods.AdminApi.Common.Infrastructure.Context;
using EdFi.Ods.AdminApi.Common.Infrastructure.MultiTenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.Audit;

[TestFixture]
public class AuditEventRecorderTests
{
    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EdFi_Admin"] = "fallback-connection-string"
            })
            .Build();

    [Test]
    public void Record_WhenAuditLoggingDisabled_DoesNotEnqueueEvent()
    {
        var channel = new AuditLogChannel();
        var tenantContext = new ContextProvider<TenantConfiguration>(new AsyncLocalContextStorage());
        var recorder = new AuditEventRecorder(
            channel,
            Options.Create(new AuditLoggingSettings { Enabled = false }),
            tenantContext,
            BuildConfiguration());

        recorder.Record(AuditEventType.Action, "client-1", "127.0.0.1", "POST", "/v3/apiClients", 201);

        channel.Reader.TryRead(out _).ShouldBeFalse();
    }

    [Test]
    public void Record_WhenAuditLoggingEnabledAndNoTenantContext_EnqueuesEventWithFallbackConnectionString()
    {
        var channel = new AuditLogChannel();
        var tenantContext = new ContextProvider<TenantConfiguration>(new AsyncLocalContextStorage());
        var recorder = new AuditEventRecorder(
            channel,
            Options.Create(new AuditLoggingSettings { Enabled = true }),
            tenantContext,
            BuildConfiguration());

        recorder.Record(AuditEventType.Action, "client-1", "127.0.0.1", "POST", "/v3/apiClients", 201);

        channel.Reader.TryRead(out var auditEvent).ShouldBeTrue();
        auditEvent!.AdminConnectionString.ShouldBe("fallback-connection-string");
        auditEvent.EventType.ShouldBe(AuditEventType.Action);
        auditEvent.ClientId.ShouldBe("client-1");
        auditEvent.SourceIpAddress.ShouldBe("127.0.0.1");
        auditEvent.HttpVerb.ShouldBe("POST");
        auditEvent.HttpUrl.ShouldBe("/v3/apiClients");
        auditEvent.StatusCode.ShouldBe(201);
    }

    [Test]
    public void Record_WhenTenantContextIsSet_EnqueuesEventWithTenantConnectionString()
    {
        var channel = new AuditLogChannel();
        var tenantContext = new ContextProvider<TenantConfiguration>(new AsyncLocalContextStorage());
        tenantContext.Set(new TenantConfiguration { AdminConnectionString = "tenant-connection-string" });
        var recorder = new AuditEventRecorder(
            channel,
            Options.Create(new AuditLoggingSettings { Enabled = true }),
            tenantContext,
            BuildConfiguration());

        recorder.Record(AuditEventType.AuthenticationFailure, null, "10.0.0.5", null, null, 401);

        channel.Reader.TryRead(out var auditEvent).ShouldBeTrue();
        auditEvent!.AdminConnectionString.ShouldBe("tenant-connection-string");
        auditEvent.ClientId.ShouldBeNull();
    }
}
