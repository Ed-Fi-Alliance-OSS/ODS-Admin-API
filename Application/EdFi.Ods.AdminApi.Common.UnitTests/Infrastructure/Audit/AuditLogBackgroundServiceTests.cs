// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using FakeItEasy;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.Audit;

[TestFixture]
public class AuditLogBackgroundServiceTests
{
    private static AuditEvent SampleEvent() => new()
    {
        AdminConnectionString = "conn",
        EventType = AuditEventType.Action,
        Timestamp = DateTime.UtcNow,
        HttpVerb = "DELETE",
        HttpUrl = "/v3/apiClients/1",
        StatusCode = 204
    };

    [Test]
    public async Task ProcessEventAsync_WhenWriterSucceedsFirstTry_WritesOnceAndDoesNotFallBack()
    {
        var writer = A.Fake<IAuditLogWriter>();
        var service = new AuditLogBackgroundService(new AuditLogChannel(), writer);

        var fellBackToLogging = await service.ProcessEventAsync(SampleEvent(), writer, CancellationToken.None);

        fellBackToLogging.ShouldBeFalse();
        A.CallTo(() => writer.WriteAsync(A<AuditEvent>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ProcessEventAsync_WhenWriterFailsTwiceThenSucceeds_RetriesAndDoesNotFallBack()
    {
        var writer = A.Fake<IAuditLogWriter>();
        var callCount = 0;
        A.CallTo(() => writer.WriteAsync(A<AuditEvent>._, A<CancellationToken>._))
            .Invokes(() => callCount++)
            .ReturnsLazily(() =>
            {
                if (callCount < 3)
                {
                    throw new InvalidOperationException("transient failure");
                }
                return Task.CompletedTask;
            });
        var service = new AuditLogBackgroundService(new AuditLogChannel(), writer);

        var fellBackToLogging = await service.ProcessEventAsync(SampleEvent(), writer, CancellationToken.None);

        fellBackToLogging.ShouldBeFalse();
        callCount.ShouldBe(3);
    }

    [Test]
    public async Task ProcessEventAsync_WhenWriterAlwaysFails_FallsBackAfterExhaustingRetries()
    {
        var writer = A.Fake<IAuditLogWriter>();
        A.CallTo(() => writer.WriteAsync(A<AuditEvent>._, A<CancellationToken>._))
            .Throws(new InvalidOperationException("permanent failure"));
        var service = new AuditLogBackgroundService(new AuditLogChannel(), writer);

        var fellBackToLogging = await service.ProcessEventAsync(SampleEvent(), writer, CancellationToken.None);

        fellBackToLogging.ShouldBeTrue();
        A.CallTo(() => writer.WriteAsync(A<AuditEvent>._, A<CancellationToken>._))
            .MustHaveHappened(3, Times.Exactly);
    }

    [Test]
    public async Task ProcessEventAsync_WhenCancelledDuringRetryDelay_FallsBackInsteadOfThrowing()
    {
        var writer = A.Fake<IAuditLogWriter>();
        A.CallTo(() => writer.WriteAsync(A<AuditEvent>._, A<CancellationToken>._))
            .Throws(new InvalidOperationException("transient failure"));
        var service = new AuditLogBackgroundService(new AuditLogChannel(), writer);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var fellBackToLogging = await service.ProcessEventAsync(SampleEvent(), writer, cts.Token);

        fellBackToLogging.ShouldBeTrue();
    }
}
