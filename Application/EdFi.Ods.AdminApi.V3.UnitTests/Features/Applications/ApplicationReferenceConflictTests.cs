// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.V3.Features.Applications;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V3.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.Applications;

[TestFixture]
public class ApplicationReferenceConflictTests
{
    private static AdminApiSqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<AdminApiSqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AppConflictV3_{Guid.NewGuid()}")
            .Options);

    private static IOptions<AppSettings> Options() =>
        Microsoft.Extensions.Options.Options.Create(new AppSettings { DatabaseEngine = "PostgreSql" });

    private static (Vendor vendor, OdsInstance dataStore) Seed(AdminApiSqlServerUsersContext ctx)
    {
        var vendor = new Vendor { VendorName = "V1" };
        var dataStore = new OdsInstance { Name = "DS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.Vendors.Add(vendor);
        ctx.OdsInstances.Add(dataStore);
        ctx.SaveChanges();
        return (vendor, dataStore);
    }

    private static AddApplication.AddApplicationRequest AddRequest(int vendorId, int dataStoreId, int[] profileIds = null) => new()
    {
        ApplicationName = "App1",
        VendorId = vendorId,
        ClaimSetName = "CS",
        EducationOrganizationIds = new long[] { 1 },
        DataStoreIds = new[] { dataStoreId },
        ProfileIds = profileIds
    };

    private static void ShouldBeUnresolved(Action action) =>
        Should.Throw<ConflictException>(action).ProblemType.ShouldBe(AdminApiProblemTypes.ConflictUnresolvedReference);

    private static void CallAdd(AdminApiSqlServerUsersContext ctx, AddApplication.AddApplicationRequest request) =>
        AddApplication.Handle(new AddApplication.Validator(A.Fake<IGetApplicationsByVendorIdQuery>()),
            A.Fake<IAddApplicationCommand>(), ctx, request, Options(), new DefaultHttpContext()).GetAwaiter().GetResult();

    [Test]
    public void AddApplication_UnknownVendor_ThrowsConflictUnresolvedReference()
    {
        using var ctx = CreateContext();
        var (_, dataStore) = Seed(ctx);

        ShouldBeUnresolved(() => CallAdd(ctx, AddRequest(9999, dataStore.OdsInstanceId)));
    }

    [Test]
    public void AddApplication_UnknownProfileId_ThrowsConflictUnresolvedReference()
    {
        using var ctx = CreateContext();
        var (vendor, dataStore) = Seed(ctx);

        ShouldBeUnresolved(() => CallAdd(ctx, AddRequest(vendor.VendorId, dataStore.OdsInstanceId, new[] { 9999 })));
    }

    [Test]
    public void AddApplication_UnknownDataStoreId_ThrowsConflictUnresolvedReference()
    {
        using var ctx = CreateContext();
        var (vendor, _) = Seed(ctx);

        ShouldBeUnresolved(() => CallAdd(ctx, AddRequest(vendor.VendorId, 9999)));
    }

    [Test]
    public void EditApplication_UnknownVendor_ThrowsConflictUnresolvedReference()
    {
        using var ctx = CreateContext();
        Seed(ctx);
        var request = new EditApplication.EditApplicationRequest
        {
            Id = 1,
            ApplicationName = "App1",
            VendorId = 9999,
            ClaimSetName = "CS",
            EducationOrganizationIds = new long[] { 1 }
        };

        ShouldBeUnresolved(() => EditApplication.Handle(
            A.Fake<IEditApplicationCommand>(), new EditApplication.Validator(), ctx, request, 1).GetAwaiter().GetResult());
    }
}
