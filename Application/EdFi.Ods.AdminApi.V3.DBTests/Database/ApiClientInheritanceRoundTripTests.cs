// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Data;
using System.Linq;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Ods.AdminApi.DBTestsShared;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.DBTests.Database;

[TestFixture]
internal class ApiClientInheritanceRoundTripTests : PlatformUsersContextTestBase
{
    protected override string AdminConnectionString => Testing.AdminConnectionString;

    [Test]
    public void NarrowedClient_RoundTripsFalse_OnSqlServer()
    {
        var vendor = new Vendor { VendorName = "Round Trip Vendor" };
        var application = new Application
        {
            ApplicationName = "Round Trip App",
            ClaimSetName = "FakeClaimSet",
            OperationalContextUri = "http://test.com",
            Vendor = vendor
        };
        var client = new ApiClient(true) { Name = "Round Trip Client" };
        Save(application);

        Transaction(ctx =>
        {
            client.Application = ctx.Applications.Single(a => a.ApplicationId == application.ApplicationId);
            ctx.ApiClients.Add(client);
        });
        Transaction(ctx =>
        {
            var tracked = ctx.ApiClients.Single(c => c.ApiClientId == client.ApiClientId);
            ctx.SetInheritsApplicationEducationOrganizations(tracked, false);
        });

        using var fresh = new AdminApiSqlServerUsersContext(GetDbContextOptions());

        var viaEf = fresh.ApiClients
            .Where(c => c.ApiClientId == client.ApiClientId)
            .Select(c => EF.Property<bool>(c, ApiClientShadowProperties.InheritsApplicationEducationOrganizations))
            .Single();
        viaEf.ShouldBeFalse();

        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT InheritsApplicationEducationOrganizations FROM dbo.ApiClients WHERE ApiClientId = @id";
        command.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = client.ApiClientId });
        ((bool)command.ExecuteScalar()!).ShouldBeFalse();
    }
}
