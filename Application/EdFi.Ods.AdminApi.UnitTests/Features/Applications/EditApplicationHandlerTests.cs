// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Features.Applications;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using FakeItEasy;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Applications;

[TestFixture]
public class EditApplicationHandlerTests
{
    private static SqlServerUsersContext CreateContext() =>
        new(new DbContextOptionsBuilder<SqlServerUsersContext>()
            .UseInMemoryDatabase(databaseName: $"EditAppHandler_{Guid.NewGuid()}")
            .Options);

    [Test]
    public async Task Handle_WithValidRequest_ReturnsOk()
    {
        using var ctx = CreateContext();
        var vendor = new Vendor { VendorName = "V1" };
        ctx.Vendors.Add(vendor);
        var ods = new OdsInstance { Name = "ODS1", InstanceType = "type", ConnectionString = "cs" };
        ctx.OdsInstances.Add(ods);
        ctx.SaveChanges();

        var fakeEditCommand = A.Fake<IEditApplicationCommand>();
        var validator = new EditApplication.Validator();
        var request = new EditApplication.EditApplicationRequest
        {
            Id = 1,
            ApplicationName = "UpdatedApp",
            VendorId = vendor.VendorId,
            ClaimSetName = "CS",
            EducationOrganizationIds = new long[] { 1 },
            OdsInstanceIds = new[] { ods.OdsInstanceId }
        };

        var result = await EditApplication.Handle(fakeEditCommand, validator, ctx, request, 1);

        result.ShouldBeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok>();
        A.CallTo(() => fakeEditCommand.Execute(A<IEditApplicationModel>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Validator_WhenApplicationNameEmpty_FailsValidation()
    {
        var validator = new EditApplication.Validator();
        var result = await validator.ValidateAsync(new EditApplication.EditApplicationRequest
        {
            ApplicationName = "",
            ClaimSetName = "CS",
            EducationOrganizationIds = new long[] { 1 },
            OdsInstanceIds = new[] { 1 }
        });
        result.IsValid.ShouldBeFalse();
    }
}
