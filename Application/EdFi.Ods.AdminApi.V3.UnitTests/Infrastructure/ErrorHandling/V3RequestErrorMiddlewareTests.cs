// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.ErrorHandling;

[TestFixture]
public class V3RequestErrorMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_WhenValidationException_ReturnsValidationProblemDetails()
    {
        var middleware = new V3RequestErrorMiddleware(_ =>
            throw new ValidationException(
                new List<ValidationFailure> { new("company", "Company is required") }
            )
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        context.Response.ContentType.ShouldContain("application/problem+json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("title").GetString().ShouldBe("Validation failed");
        doc.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        doc.RootElement.TryGetProperty("validationErrors", out _).ShouldBeTrue();
        doc.RootElement.GetProperty("type").GetString().ShouldBe(AdminApiProblemTypes.BadRequestValidation);
    }

    [Test]
    public async Task InvokeAsync_WhenNotFoundException_ReturnsProblemDetails()
    {
        var middleware = new V3RequestErrorMiddleware(_ =>
            throw new NotFoundException<string>("Vendor", "123")
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        context.Response.ContentType.ShouldContain("application/problem+json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetInt32().ShouldBe(404);
        doc.RootElement.GetProperty("title").GetString()!.ToLowerInvariant().ShouldContain("not found");
        doc.RootElement.GetProperty("detail").GetString()!.ToLowerInvariant().ShouldContain("not found");
        doc.RootElement.GetProperty("type").GetString().ShouldBe(AdminApiProblemTypes.NotFound);
    }

    [Test]
    public async Task InvokeAsync_WhenBadHttpRequestException_ReturnsBadRequestWithDataType()
    {
        var middleware = new V3RequestErrorMiddleware(_ =>
            throw new BadHttpRequestException("Malformed JSON")
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        context.Response.ContentType.ShouldContain("application/problem+json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        doc.RootElement.GetProperty("type").GetString().ShouldBe(AdminApiProblemTypes.BadRequestData);
    }

    [Test]
    public async Task InvokeAsync_WhenAdminApiException_With4xxStatus_ReturnsBadRequestType()
    {
        var middleware = new V3RequestErrorMiddleware(_ =>
            throw new AdminApiException("Unprocessable entity") { StatusCode = HttpStatusCode.UnprocessableEntity }
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status422UnprocessableEntity);
        context.Response.ContentType.ShouldContain("application/problem+json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetInt32().ShouldBe(422);
        doc.RootElement.GetProperty("type").GetString().ShouldBe(AdminApiProblemTypes.BadRequest);
    }

    [Test]
    public async Task InvokeAsync_WhenAdminApiException_With5xxStatus_ReturnsInternalServerErrorType()
    {
        var middleware = new V3RequestErrorMiddleware(_ =>
            throw new AdminApiException("Something went wrong") { StatusCode = HttpStatusCode.InternalServerError }
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.ShouldContain("application/problem+json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetInt32().ShouldBe(500);
        doc.RootElement.GetProperty("type").GetString().ShouldBe(AdminApiProblemTypes.InternalServerError);
    }

    [Test]
    public async Task InvokeAsync_WhenPipelineReturnsBadRequestWithEmptyBody_WritesProblemDetailsWithBadRequestDataType()
    {
        var middleware = new V3RequestErrorMiddleware(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/v3/claimSets";
        context.Request.ContentType = "application/json";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        context.Response.ContentType.ShouldContain("application/problem+json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("title").GetString().ShouldBe("Bad Request");
        doc.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        doc.RootElement.GetProperty("type").GetString().ShouldBe(AdminApiProblemTypes.BadRequestData);
    }

    [Test]
    public async Task InvokeAsync_WhenPipelineAlreadyReturnsProblemDetails_DoesNotOverwriteBody()
    {
        const string existingBody =
            "{\"type\":\"urn:ed-fi:admin-api:bad-request:version-mismatch\",\"title\":\"Bad Request\",\"status\":400,\"detail\":\"Wrong API version for this instance mode.\"}";

        var middleware = new V3RequestErrorMiddleware(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(existingBody);
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/v3/tenants/default/DataStores/edOrgs";
        context.Request.ContentType = "application/json";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        context.Response.ContentType.ShouldContain("application/problem+json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
        body.ShouldBe(existingBody);
    }

    [Test]
    public async Task InvokeAsync_WhenTokenEndpointReturnsInvalidScope_SetsProblemJsonContentTypeAndPreservesBody()
    {
        const string existingBody = "{\"error\":\"invalid_scope\",\"error_description\":\"Invalid scope.\"}";

        var middleware = new V3RequestErrorMiddleware(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(existingBody);
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/connect/token";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        context.Response.ContentType.ShouldBe("application/problem+json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
        body.ShouldBe(existingBody);
    }

    [Test]
    public async Task InvokeAsync_WhenTokenEndpointReturnsSuccess_CopiesResponseBodyWithoutChangingContentType()
    {
        const string existingBody = "{\"access_token\":\"token-value\"}";

        var middleware = new V3RequestErrorMiddleware(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(existingBody);
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/connect/token";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        context.Response.ContentType.ShouldBe("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
        body.ShouldBe(existingBody);
    }
}
