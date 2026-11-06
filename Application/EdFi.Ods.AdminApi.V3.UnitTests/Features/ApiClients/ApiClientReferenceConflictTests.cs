// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Features.ApiClients;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ApiClients;

[TestFixture]
public class ApiClientReferenceConflictTests
{
    private static AdminApiSqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<AdminApiSqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"ApiClientConflictV3_{Guid.NewGuid()}")
            .Options);

    private static IOptions<AppSettings> Options() =>
        Microsoft.Extensions.Options.Options.Create(new AppSettings { DatabaseEngine = "PostgreSql" });

    private static (Application application, OdsInstance dataStore) Seed(AdminApiSqlServerUsersContext ctx)
    {
        var vendor = new Vendor { VendorName = "V1" };
        var application = new Application { ApplicationName = "App1", ClaimSetName = "CS", Vendor = vendor, OperationalContextUri = "uri" };
        var dataStore = new OdsInstance { Name = "DS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.Vendors.Add(vendor);
        ctx.Applications.Add(application);
        ctx.OdsInstances.Add(dataStore);
        ctx.SaveChanges();
        return (application, dataStore);
    }

    private static void CallAdd(AdminApiSqlServerUsersContext ctx, IGetApiClientsByApplicationIdQuery query, AddApiClient.AddApiClientRequest request) =>
        AddApiClient.Handle(new AddApiClient.Validator(), A.Fake<IAddApiClientCommand>(), query,
            ctx, request, Options(), new DefaultHttpContext()).GetAwaiter().GetResult();

    private static void CallEdit(AdminApiSqlServerUsersContext ctx, EditApiClient.EditApiClientRequest request) =>
        EditApiClient.Handle(A.Fake<IEditApiClientCommand>(), new EditApiClient.Validator(), ctx, request, 1)
            .GetAwaiter().GetResult();

    [Test]
    public void AddApiClient_UnknownApplication_ThrowsConflictUnresolvedReference()
    {
        using var ctx = CreateContext();
        var (_, dataStore) = Seed(ctx);
        var request = new AddApiClient.AddApiClientRequest { Name = "C1", ApplicationId = 9999, DataStoreIds = new[] { dataStore.OdsInstanceId } };

        var ex = Should.Throw<ConflictException>(() => CallAdd(ctx, A.Fake<IGetApiClientsByApplicationIdQuery>(), request));

        ex.ProblemType.ShouldBe(AdminApiProblemTypes.ConflictUnresolvedReference);
        ex.Message.ShouldBe("Application with ID 9999 not found.");
    }

    [Test]
    public void AddApiClient_UnknownDataStoreId_ThrowsConflictUnresolvedReference()
    {
        using var ctx = CreateContext();
        var (application, _) = Seed(ctx);
        var request = new AddApiClient.AddApiClientRequest { Name = "C1", ApplicationId = application.ApplicationId, DataStoreIds = new[] { 9999 } };

        var ex = Should.Throw<ConflictException>(() => CallAdd(ctx, A.Fake<IGetApiClientsByApplicationIdQuery>(), request));

        ex.ProblemType.ShouldBe(AdminApiProblemTypes.ConflictUnresolvedReference);
    }

    [Test]
    public void AddApiClient_DuplicateNameInApplication_ThrowsConflict()
    {
        using var ctx = CreateContext();
        var (application, dataStore) = Seed(ctx);
        var query = A.Fake<IGetApiClientsByApplicationIdQuery>();
        A.CallTo(() => query.ExistsByApplicationIdAndName(application.ApplicationId, "C1")).Returns(true);
        var request = new AddApiClient.AddApiClientRequest { Name = " C1 ", ApplicationId = application.ApplicationId, DataStoreIds = new[] { dataStore.OdsInstanceId } };

        var ex = Should.Throw<ConflictException>(() => CallAdd(ctx, query, request));

        ex.ProblemType.ShouldBe(AdminApiProblemTypes.Conflict);
        ex.Message.ShouldBe(FeatureConstants.ApiClientCombinedKeyMustBeUnique);
    }

    [Test]
    public void EditApiClient_UnknownApplication_ThrowsConflictUnresolvedReference()
    {
        using var ctx = CreateContext();
        var (_, dataStore) = Seed(ctx);
        var request = new EditApiClient.EditApiClientRequest { Id = 1, Name = "C1", ApplicationId = 9999, DataStoreIds = new[] { dataStore.OdsInstanceId } };

        var ex = Should.Throw<ConflictException>(() => CallEdit(ctx, request));

        ex.ProblemType.ShouldBe(AdminApiProblemTypes.ConflictUnresolvedReference);
        ex.Message.ShouldBe("Application with ID 9999 not found.");
    }

    [Test]
    public void EditApiClient_UnknownDataStoreId_ThrowsConflictUnresolvedReference()
    {
        using var ctx = CreateContext();
        var (application, _) = Seed(ctx);
        var request = new EditApiClient.EditApiClientRequest { Id = 1, Name = "C1", ApplicationId = application.ApplicationId, DataStoreIds = new[] { 9999 } };

        var ex = Should.Throw<ConflictException>(() => CallEdit(ctx, request));

        ex.ProblemType.ShouldBe(AdminApiProblemTypes.ConflictUnresolvedReference);
    }
}
