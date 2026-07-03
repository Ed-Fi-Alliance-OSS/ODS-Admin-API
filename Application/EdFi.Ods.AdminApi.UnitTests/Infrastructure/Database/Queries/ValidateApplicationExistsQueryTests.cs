// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Infrastructure.Database.Queries;

[TestFixture]
public class ValidateApplicationExistsQueryTests
{
    private sealed class AddApplicationModelStub : IAddApplicationModel
    {
        public string? ApplicationName { get; init; }
        public int VendorId { get; init; }
        public string? ClaimSetName { get; init; }
        public IEnumerable<int>? ProfileIds { get; init; }
        public IEnumerable<long>? EducationOrganizationIds { get; init; }
        public IEnumerable<int>? OdsInstanceIds { get; init; }
        public bool? Enabled { get; init; }
    }

    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"ValidateApplicationExistsQueryTests_{Guid.NewGuid()}")
            .Options);

    private static Application CreateApplication(Vendor vendor)
    {
        return new Application
        {
            ApplicationName = "Test App",
            ClaimSetName = "ClaimSet",
            OperationalContextUri = "uri",
            Vendor = vendor,
            ApplicationEducationOrganizations = new List<ApplicationEducationOrganization>(),
            Profiles = new List<Profile>(),
            ApiClients = new List<ApiClient>()
        };
    }

    [Test]
    public void Execute_WithNoMatchingApplication_ReturnsFalse()
    {
        using var context = CreateContext();
        var query = new ValidateApplicationExistsQuery(context);

        var result = query.Execute(new AddApplicationModelStub
        {
            ApplicationName = "Missing App",
            ClaimSetName = "ClaimSet",
            VendorId = 999,
            EducationOrganizationIds = Array.Empty<long>(),
            ProfileIds = Array.Empty<int>(),
            OdsInstanceIds = Array.Empty<int>()
        });

        result.ShouldBeFalse();
    }

    [Test]
    public void Execute_WithExactDuplicateApplication_ReturnsTrue()
    {
        using var context = CreateContext();
        var vendor = new Vendor { VendorName = "Acme Vendor" };
        var application = CreateApplication(vendor);
        var apiClient = new ApiClient(true)
        {
            Name = "Client One",
            Application = application,
            User = new User { FullName = "Client User", Email = "client@test.org" }
        };
        application.ApiClients.Add(apiClient);
        context.Applications.Add(application);
        context.SaveChanges();

        var query = new ValidateApplicationExistsQuery(context);

        var result = query.Execute(new AddApplicationModelStub
        {
            ApplicationName = application.ApplicationName,
            ClaimSetName = application.ClaimSetName,
            VendorId = vendor.VendorId,
            EducationOrganizationIds = Array.Empty<long>(),
            ProfileIds = Array.Empty<int>(),
            OdsInstanceIds = Array.Empty<int>()
        });

        result.ShouldBeTrue();
    }
}
