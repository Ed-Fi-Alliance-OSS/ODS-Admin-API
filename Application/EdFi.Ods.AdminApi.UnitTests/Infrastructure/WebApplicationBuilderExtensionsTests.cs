// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Net;
using System.Threading.RateLimiting;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure;

[TestFixture]
public class WebApplicationBuilderExtensionsTests
{
    [Test]
    public void ConfigureRateLimiting_WhenDisabled_ShouldConfigureNoLimiter()
    {
        // Arrange
        var builder = CreateWebApplicationBuilder(false);

        // Act
        WebApplicationBuilderExtensions.ConfigureRateLimiting(builder);

        // Assert
        var services = builder.Services.BuildServiceProvider();
        var options = services.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        options.ShouldNotBeNull();

        // Build an app to test the global limiter
        var app = builder.Build();
        var httpContext = new DefaultHttpContext();
        var globalLimiter = app.Services.GetRequiredService<IOptions<RateLimiterOptions>>().Value.GlobalLimiter;

        globalLimiter.ShouldNotBeNull();

        // The global limiter should be configured as a NoLimiter when disabled
        var acquireResult = globalLimiter.AttemptAcquire(httpContext);
        acquireResult.IsAcquired.ShouldBeTrue();
    }

    [Test]
    public void ConfigureRateLimiting_WhenEnabled_ShouldConfigureRejectionStatusCode()
    {
        // Arrange
        var builder = CreateWebApplicationBuilder(true, httpStatusCode: 429);

        // Act
        WebApplicationBuilderExtensions.ConfigureRateLimiting(builder);

        // Assert
        var services = builder.Services.BuildServiceProvider();
        var options = services.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        options.ShouldNotBeNull();
        options.RejectionStatusCode.ShouldBe(429);
    }

    [Test]
    public void ConfigureRateLimiting_WithEndpointRules_ShouldConfigureEndpointSpecificLimiters()
    {
        // Arrange
        var builder = CreateWebApplicationBuilder(true, new[]
        {
            new GeneralRule { Endpoint = "POST:/Connect/Register", Period = "1m", Limit = 3 }
        });

        // Act
        WebApplicationBuilderExtensions.ConfigureRateLimiting(builder);

        // Assert
        var services = builder.Services.BuildServiceProvider();
        var options = services.GetRequiredService<IOptions<RateLimiterOptions>>().Value;

        options.ShouldNotBeNull();

        // Build an app to test the endpoint limiters
        var app = builder.Build();
        var globalLimiter = app.Services.GetRequiredService<IOptions<RateLimiterOptions>>().Value.GlobalLimiter;

        globalLimiter.ShouldNotBeNull();

        // Test with a matching endpoint
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/Connect/Register";

        // First 3 requests should be allowed for the matching endpoint
        for (int i = 0; i < 3; i++)
        {
            var acquireResult = globalLimiter.AttemptAcquire(httpContext);
            acquireResult.IsAcquired.ShouldBeTrue($"Request {i + 1} should be allowed");
        }

        // 4th request should be rejected
        var fourthResult = globalLimiter.AttemptAcquire(httpContext);
        fourthResult.IsAcquired.ShouldBeFalse("4th request should be rejected");

        // Test with a non-matching endpoint
        var differentContext = new DefaultHttpContext();
        differentContext.Request.Method = "GET";
        differentContext.Request.Path = "/api/different";

        var differentResult = globalLimiter.AttemptAcquire(differentContext);
        differentResult.IsAcquired.ShouldBeTrue("Non-matching endpoint should be allowed");
    }

    private static WebApplicationBuilder CreateWebApplicationBuilder(
        bool enableEndpointRateLimiting,
        IEnumerable<GeneralRule> generalRules = null,
        int httpStatusCode = (int)HttpStatusCode.TooManyRequests)
    {
        var configValues = new Dictionary<string, string>
        {
            { "IpRateLimiting:EnableEndpointRateLimiting", enableEndpointRateLimiting.ToString() },
            { "IpRateLimiting:HttpStatusCode", httpStatusCode.ToString() }
        };

        if (generalRules != null)
        {
            int index = 0;
            foreach (var rule in generalRules)
            {
                configValues[$"IpRateLimiting:GeneralRules:{index}:Endpoint"] = rule.Endpoint;
                configValues[$"IpRateLimiting:GeneralRules:{index}:Period"] = rule.Period;
                configValues[$"IpRateLimiting:GeneralRules:{index}:Limit"] = rule.Limit.ToString();
                index++;
            }
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddConfiguration(configuration);
        return builder;
    }

    private class GeneralRule
    {
        public string Endpoint { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public int Limit { get; set; }
    }
}
