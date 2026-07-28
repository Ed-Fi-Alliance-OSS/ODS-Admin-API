// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Security.Claims;
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace EdFi.Ods.AdminApi.Common.UnitTests.Infrastructure.Audit;

[TestFixture]
public class AuditActionLoggingMiddlewareTests
{
    private static DefaultHttpContext BuildContext(string method, string path, string? clientId, int statusCode)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        context.Response.StatusCode = statusCode;
        if (clientId != null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("client_id", clientId)], "test"));
        }
        return context;
    }

    [Test]
    public async Task InvokeAsync_ForPostRequest_RecordsActionEvent()
    {
        var recorder = A.Fake<IAuditEventRecorder>();
        var middleware = new AuditActionLoggingMiddleware(_ => Task.CompletedTask, recorder);
        var context = BuildContext("POST", "/v3/apiClients", "client-1", 201);

        await middleware.InvokeAsync(context);

        A.CallTo(() => recorder.Record(
            AuditEventType.Action, "client-1", A<string?>._, "POST", "/v3/apiClients", 201))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task InvokeAsync_ForGetRequest_DoesNotRecordEvent()
    {
        var recorder = A.Fake<IAuditEventRecorder>();
        var middleware = new AuditActionLoggingMiddleware(_ => Task.CompletedTask, recorder);
        var context = BuildContext("GET", "/v3/apiClients", "client-1", 200);

        await middleware.InvokeAsync(context);

        A.CallTo(() => recorder.Record(
            A<AuditEventType>._, A<string?>._, A<string?>._, A<string?>._, A<string?>._, A<int?>._))
            .MustNotHaveHappened();
    }

    [Test]
    public async Task InvokeAsync_WhenNoClientIdClaim_RecordsNullClientId()
    {
        var recorder = A.Fake<IAuditEventRecorder>();
        var middleware = new AuditActionLoggingMiddleware(_ => Task.CompletedTask, recorder);
        var context = BuildContext("DELETE", "/v3/apiClients/1", null, 204);

        await middleware.InvokeAsync(context);

        A.CallTo(() => recorder.Record(
            AuditEventType.Action, null, A<string?>._, "DELETE", "/v3/apiClients/1", 204))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public void InvokeAsync_WhenNextThrows_StillRecordsActionEventAndPropagatesException()
    {
        var recorder = A.Fake<IAuditEventRecorder>();
        var middleware = new AuditActionLoggingMiddleware(
            _ => throw new InvalidOperationException("downstream failure"), recorder);
        var context = BuildContext("POST", "/v3/apiClients", "client-1", 200);

        Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        A.CallTo(() => recorder.Record(
            AuditEventType.Action, "client-1", A<string?>._, "POST", "/v3/apiClients", A<int?>._))
            .MustHaveHappenedOnceExactly();
    }
}
