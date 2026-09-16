// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;
using System.Net;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Infrastructure.Database.Commands;

[TestFixture]
public class RegenerateApplicationApiClientSecretCommandTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"RegenAppSecretCmdV3_{Guid.NewGuid()}")
            .Options);

    [Test]
    public void Execute_WithTwoApiClients_ThrowsConflictDirectingToApiClientEndpoint()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.ApiClients.Add(new ApiClient(true) { Name = "cred-a", Application = app });
        ctx.ApiClients.Add(new ApiClient(true) { Name = "cred-b", Application = app });
        ctx.SaveChanges();

        var exception = Should.Throw<AdminApiException>(() =>
            new RegenerateApplicationApiClientSecretCommand(ctx).Execute(app.ApplicationId));

        exception.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        exception.Message.ShouldBe(FeatureConstants.ApplicationResetCredentialMultiClientConflictMessage);
    }

    [Test]
    public void Execute_WithTwoApiClients_DoesNotRegenerateEitherCredentialSecret()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        ctx.ApiClients.Add(new ApiClient(true) { Name = "cred-a", Application = app });
        ctx.ApiClients.Add(new ApiClient(true) { Name = "cred-b", Application = app });
        ctx.SaveChanges();
        var originalSecrets = ctx.ApiClients.Select(c => c.Secret).OrderBy(s => s).ToList();

        Should.Throw<AdminApiException>(() =>
            new RegenerateApplicationApiClientSecretCommand(ctx).Execute(app.ApplicationId));

        ctx.ApiClients.Select(c => c.Secret).OrderBy(s => s).ToList().ShouldBe(originalSecrets);
    }

    [Test]
    public void Execute_WithOneApiClient_RegeneratesItsSecret()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var app = new Application { ApplicationName = "App", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        ctx.Applications.Add(app);
        var apiClient = new ApiClient(true) { Name = "cred-a", Application = app };
        ctx.ApiClients.Add(apiClient);
        ctx.SaveChanges();
        var originalSecret = apiClient.Secret;

        var result = new RegenerateApplicationApiClientSecretCommand(ctx).Execute(app.ApplicationId);

        result.Secret.ShouldNotBe(originalSecret);
        ctx.ApiClients.Single().Secret.ShouldBe(result.Secret);
    }
}
