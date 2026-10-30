// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.Common.Infrastructure.Database;
using EdFi.Admin.DataAccess.Models;
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
public class AddApplicationHandlerTests
{
    private static AdminApiSqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<AdminApiSqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddAppV3Handler_{Guid.NewGuid()}")
            .Options);
    private static IOptions<AppSettings> Options() =>
        Microsoft.Extensions.Options.Options.Create(new AppSettings { DatabaseEngine = "PostgreSql" });

    [Test]
    public async Task Handle_WithValidRequest_ReturnsCreated()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var ods = new OdsInstance { Name = "DS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();

        var fakeAddCommand = A.Fake<IAddApplicationCommand>();
        A.CallTo(() => fakeAddCommand.Execute(A<IAddApplicationModel>._, A<IOptions<AppSettings>>._))
            .Returns(new AddApplicationResult { ApplicationId = 1, Key = "k", Secret = "s" });

        var validator = new AddApplication.Validator(new GetApplicationsByVendorIdQuery(ctx));
        var request = new AddApplication.AddApplicationRequest
        {
            ApplicationName = "App1",
            VendorId = vendor.VendorId,
            ClaimSetName = "CS",
            EducationOrganizationIds = new long[] { 1 },
            DataStoreIds = new[] { ods.OdsInstanceId }
        };

        var fakeHttpContext = new DefaultHttpContext();
        fakeHttpContext.Request.Scheme = "https";
        fakeHttpContext.Request.Host = new HostString("localhost");

        var result = await AddApplication.Handle(validator, fakeAddCommand, ctx, request, Options(), fakeHttpContext);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Created<ApplicationResult>>();
    }

    [Test]
    public async Task Validator_WhenApplicationNameEmpty_FailsValidation()
    {
        var fakeGetApplicationsByVendorIdQuery = A.Fake<IGetApplicationsByVendorIdQuery>();
        A.CallTo(() => fakeGetApplicationsByVendorIdQuery.ExistsByVendorIdAndName(A<int>._, A<string>._)).Returns(false);
        var validator = new AddApplication.Validator(fakeGetApplicationsByVendorIdQuery);
        var result = await validator.ValidateAsync(new AddApplication.AddApplicationRequest
        {
            ApplicationName = "",
            ClaimSetName = "CS",
            EducationOrganizationIds = new long[] { 1 },
            DataStoreIds = new[] { 1 }
        });
        result.IsValid.ShouldBeFalse();
    }

    private static async Task<BadRequestDataException> HandleFalseAsync(bool? enabled, IEnumerable<int> dataStoreIds)
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        ctx.SaveChanges();

        var validator = new AddApplication.Validator(new GetApplicationsByVendorIdQuery(ctx));
        var request = new AddApplication.AddApplicationRequest
        {
            ApplicationName = "App1",
            VendorId = vendor.VendorId,
            ClaimSetName = "CS",
            EducationOrganizationIds = new long[] { 1 },
            DataStoreIds = dataStoreIds,
            Enabled = enabled,
            CreateDefaultCredentials = false
        };

        return await Should.ThrowAsync<BadRequestDataException>(() => AddApplication.Handle(
            validator, A.Fake<IAddApplicationCommand>(), ctx, request, Options(), new DefaultHttpContext()));
    }

    [Test]
    public async Task Handle_FalseWithDataStoreIds_ThrowsBadRequestDataOnDataStoreIds()
    {
        var ex = await HandleFalseAsync(null, new[] { 999 });
        ex.PropertyName.ShouldBe("dataStoreIds");
    }

    [Test]
    public async Task Handle_FalseWithEmptyDataStoreIds_ThrowsBadRequestDataOnDataStoreIds()
    {
        var ex = await HandleFalseAsync(null, new int[0]);
        ex.PropertyName.ShouldBe("dataStoreIds");
    }

    [Test]
    public async Task Handle_FalseWithEnabled_ThrowsBadRequestDataOnEnabled()
    {
        var ex = await HandleFalseAsync(true, null);
        ex.PropertyName.ShouldBe("enabled");
    }

    [Test]
    public async Task Handle_FalseWithoutDataStoreIds_ReturnsCreatedWithNullKeyAndSecret()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        ctx.SaveChanges();

        var fakeAddCommand = A.Fake<IAddApplicationCommand>();
        A.CallTo(() => fakeAddCommand.Execute(A<IAddApplicationModel>._, A<IOptions<AppSettings>>._))
            .Returns(new AddApplicationResult { ApplicationId = 1 });
        var validator = new AddApplication.Validator(new GetApplicationsByVendorIdQuery(ctx));
        var request = new AddApplication.AddApplicationRequest
        {
            ApplicationName = "App1",
            VendorId = vendor.VendorId,
            ClaimSetName = "CS",
            EducationOrganizationIds = new long[] { 1 },
            CreateDefaultCredentials = false
        };
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");

        var result = await AddApplication.Handle(validator, fakeAddCommand, ctx, request, Options(), httpContext);

        var created = result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Created<ApplicationResult>>();
        created.Value!.Key.ShouldBeNull();
        created.Value.Secret.ShouldBeNull();
    }

    [Test]
    public async Task Validator_FalseWithoutDataStoreIds_IsValid()
    {
        var query = A.Fake<IGetApplicationsByVendorIdQuery>();
        A.CallTo(() => query.ExistsByVendorIdAndName(A<int>._, A<string>._)).Returns(false);
        var result = await new AddApplication.Validator(query).ValidateAsync(new AddApplication.AddApplicationRequest
        {
            ApplicationName = "App", VendorId = 1, ClaimSetName = "CS",
            EducationOrganizationIds = new long[] { 1 }, CreateDefaultCredentials = false
        });
        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task Validator_OmittedWithoutDataStoreIds_IsInvalid()
    {
        var query = A.Fake<IGetApplicationsByVendorIdQuery>();
        A.CallTo(() => query.ExistsByVendorIdAndName(A<int>._, A<string>._)).Returns(false);
        var result = await new AddApplication.Validator(query).ValidateAsync(new AddApplication.AddApplicationRequest
        {
            ApplicationName = "App", VendorId = 1, ClaimSetName = "CS",
            EducationOrganizationIds = new long[] { 1 }
        });
        result.IsValid.ShouldBeFalse();
    }
}
