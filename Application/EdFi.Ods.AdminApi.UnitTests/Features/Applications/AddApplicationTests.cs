// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Applications;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Applications;

[TestFixture]
public class AddApplicationTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"AddApp_{Guid.NewGuid()}")
            .Options);
    private static IOptions<AppSettings> Options() =>
        Microsoft.Extensions.Options.Options.Create(new AppSettings { DatabaseEngine = "PostgreSql" });

    [Test]
    public async Task Handle_WithValidRequest_ReturnsCreated()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();

        var fakeAddCommand = A.Fake<IAddApplicationCommand>();
        A.CallTo(() => fakeAddCommand.Execute(A<IAddApplicationModel>._, A<IOptions<AppSettings>>._))
            .Returns(new AddApplicationResult { ApplicationId = 1, Key = "key", Secret = "secret" });

        var validator = new AddApplication.Validator();
        var request = new AddApplication.AddApplicationRequest
        {
            ApplicationName = "App1",
            VendorId = vendor.VendorId,
            ClaimSetName = "CS",
            EducationOrganizationIds = new long[] { 1 },
            OdsInstanceIds = new[] { ods.OdsInstanceId }
        };

        var result = await AddApplication.Handle(validator, fakeAddCommand, ctx, request, Options());

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Created<ApplicationResult>>();
    }

    [Test]
    public async Task Validator_WhenApplicationNameEmpty_FailsValidation()
    {
        var validator = new AddApplication.Validator();
        var result = await validator.ValidateAsync(new AddApplication.AddApplicationRequest
        {
            ApplicationName = "",
            ClaimSetName = "CS",
            EducationOrganizationIds = new long[] { 1 },
            OdsInstanceIds = new[] { 1 }
        });
        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_WhenVendorNotFound_ThrowsValidationException()
    {
        using var ctx = CreateContext();
        ctx.SaveChanges();
        var fakeAddCommand = A.Fake<IAddApplicationCommand>();
        var validator = new AddApplication.Validator();
        var request = new AddApplication.AddApplicationRequest
        {
            ApplicationName = "App1",
            VendorId = 9999,
            ClaimSetName = "CS",
            EducationOrganizationIds = new long[] { 1 },
            OdsInstanceIds = new[] { 1 }
        };
        Should.Throw<FluentValidation.ValidationException>(
            async () => await AddApplication.Handle(validator, fakeAddCommand, ctx, request, Options()));
    }
}
