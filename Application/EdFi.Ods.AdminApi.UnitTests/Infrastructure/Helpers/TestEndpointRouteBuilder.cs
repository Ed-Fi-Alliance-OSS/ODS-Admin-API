// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using FakeItEasy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Helpers;

internal class TestEndpointRouteBuilder : IEndpointRouteBuilder
{
    public IServiceProvider ServiceProvider { get; }
    public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();

    public TestEndpointRouteBuilder()
    {
        var fakeServiceProvider = A.Fake<IServiceProvider>();
        var fakeRouteHandlerOptions = A.Fake<IOptions<RouteHandlerOptions>>();

        A.CallTo(() => fakeServiceProvider.GetService(typeof(IOptions<RouteHandlerOptions>)))
            .Returns(fakeRouteHandlerOptions);

        ServiceProvider = fakeServiceProvider;
    }

    public IApplicationBuilder CreateApplicationBuilder() => A.Fake<IApplicationBuilder>();
}
