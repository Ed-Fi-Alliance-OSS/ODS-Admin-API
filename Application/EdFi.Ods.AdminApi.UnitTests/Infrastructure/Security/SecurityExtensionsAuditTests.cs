// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using EdFi.Ods.AdminApi.Common.Infrastructure.Audit;
using EdFi.Ods.AdminApi.Infrastructure.Security;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Security;

[TestFixture]
public class SecurityExtensionsAuditTests
{
    private static OpenIddictServerTransaction BuildTransaction(HttpContext httpContext)
    {
        var transaction = new OpenIddictServerTransaction
        {
            Request = new OpenIddictRequest()
        };

        if (httpContext is not null)
        {
            transaction.Properties[typeof(HttpRequest).FullName!] = httpContext.Request;
        }

        return transaction;
    }

    [Test]
    public void DefaultTokenResponseHandler_HandleAsync_WhenNoError_RecordsAuthenticationSuccess()
    {
        var recorder = A.Fake<IAuditEventRecorder>();
        var handler = new SecurityExtensions.DefaultTokenResponseHandler(recorder);
        var transaction = BuildTransaction(httpContext: null);
        transaction.Request!.ClientId = "client-1";
        var context = new ApplyTokenResponseContext(transaction)
        {
            Response = new OpenIddictResponse()
        };

        _ = handler.HandleAsync(context);

        A.CallTo(() => recorder.Record(
            AuditEventType.AuthenticationSuccess, "client-1", A<string>._, null, null, null))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public void DefaultTokenResponseHandler_HandleAsync_WhenErrorSet_RecordsAuthenticationFailure()
    {
        var recorder = A.Fake<IAuditEventRecorder>();
        var handler = new SecurityExtensions.DefaultTokenResponseHandler(recorder);
        var transaction = BuildTransaction(httpContext: null);
        transaction.Request!.ClientId = "client-1";
        var context = new ApplyTokenResponseContext(transaction)
        {
            Response = new OpenIddictResponse
            {
                Error = OpenIddictConstants.Errors.InvalidGrant
            }
        };

        _ = handler.HandleAsync(context);

        A.CallTo(() => recorder.Record(
            AuditEventType.AuthenticationFailure, "client-1", A<string>._, null, null, null))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public void RecordChallengeAuditEvent_AlwaysRecordsAuthenticationFailureWithNullClientIdAnd401()
    {
        // SecurityExtensions.AddSecurityUsingOpenIddict wires up
        // JwtBearerEvents.OnChallenge as a one-line lambda that delegates to
        // SecurityExtensions.RecordChallengeAuditEvent(context.HttpContext).
        // Constructing a full JwtBearerChallengeContext (which requires an
        // AuthenticationScheme + JwtBearerOptions + AuthenticationProperties
        // object graph) adds no verification value beyond confirming the
        // lambda forwards HttpContext, so this test exercises the extracted
        // method directly with a real HttpContext instead.
        var recorder = A.Fake<IAuditEventRecorder>();
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        SecurityExtensions.RecordChallengeAuditEvent(httpContext);

        A.CallTo(() => recorder.Record(
            AuditEventType.AuthenticationFailure,
            null,
            A<string>._,
            null,
            null,
            (int)HttpStatusCode.Unauthorized))
            .MustHaveHappenedOnceExactly();
    }
}
