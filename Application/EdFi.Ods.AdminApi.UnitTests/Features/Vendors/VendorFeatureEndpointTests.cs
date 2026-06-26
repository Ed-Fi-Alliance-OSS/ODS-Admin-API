// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Features.Vendors;
using EdFi.Ods.AdminApi.UnitTests.Infrastructure.Helpers;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Vendors;

[TestFixture]
public class VendorFeatureEndpointTests
{
    [Test]
    public void AddVendor_MapEndpoints_RegistersRoutesWithoutThrowing()
    {
        var builder = new TestEndpointRouteBuilder();
        var feature = new AddVendor();
        Assert.DoesNotThrow(() => feature.MapEndpoints(builder));
        builder.DataSources.ShouldNotBeEmpty();
    }

    [Test]
    public void DeleteVendor_MapEndpoints_RegistersRoutesWithoutThrowing()
    {
        var builder = new TestEndpointRouteBuilder();
        var feature = new DeleteVendor();
        Assert.DoesNotThrow(() => feature.MapEndpoints(builder));
        builder.DataSources.ShouldNotBeEmpty();
    }

    [Test]
    public void EditVendor_MapEndpoints_RegistersRoutesWithoutThrowing()
    {
        var builder = new TestEndpointRouteBuilder();
        var feature = new EditVendor();
        Assert.DoesNotThrow(() => feature.MapEndpoints(builder));
        builder.DataSources.ShouldNotBeEmpty();
    }

    [Test]
    public void ReadVendor_MapEndpoints_RegistersRoutesWithoutThrowing()
    {
        var builder = new TestEndpointRouteBuilder();
        var feature = new ReadVendor();
        Assert.DoesNotThrow(() => feature.MapEndpoints(builder));
        builder.DataSources.ShouldNotBeEmpty();
    }
}
