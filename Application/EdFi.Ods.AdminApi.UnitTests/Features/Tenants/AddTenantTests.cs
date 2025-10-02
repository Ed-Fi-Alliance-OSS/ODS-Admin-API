// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Threading.Tasks;
using AutoMapper;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Tenants;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Tenants;

[TestFixture]
public class AddTenantTests
{
    [Test]
    public async Task Handle_ReturnsBadRequest_WhenNotMultiTenant()
    {
        var options = A.Fake<IOptions<AppSettings>>();
        A.CallTo(() => options.Value).Returns(new AppSettings { MultiTenancy = false, DatabaseEngine = "Postgres" });

        var addTenantCommand = new TestAddTenantCommand();
        var mapper = A.Fake<IMapper>();
        var request = new AddTenant.AddTenantRequest { TenantName = "tenant1" };
        var serviceScopeFactory = A.Fake<IServiceScopeFactory>();
        var tenantsService = A.Fake<ITenantsService>();
        var scope = A.Fake<IServiceScope>();
        var scopedServiceProvider = A.Fake<IServiceProvider>();

        A.CallTo(() => scopedServiceProvider.GetService(typeof(ITenantsService))).Returns(tenantsService);

        A.CallTo(() => serviceScopeFactory.CreateScope()).Returns(scope);
        A.CallTo(() => scope.ServiceProvider).Returns(scopedServiceProvider);

        var validator = new TestValidator(tenantsService, options);

        var result = await AddTenant.Handle(validator, addTenantCommand, mapper, request, serviceScopeFactory, options);

        // Assert
        var badRequest = result as IResult;
        badRequest.ShouldNotBeNull();
        badRequest.GetType().Name.ShouldStartWith("BadRequest");
        var valueProperty = badRequest.GetType().GetProperty("Value");
        valueProperty.ShouldNotBeNull();
        var value = valueProperty.GetValue(badRequest);
        value.ShouldNotBeNull();
        value.ToString().ShouldContain("Not multitenant environment.");
    }

    [Test]
    public async Task Handle_CallsValidatorAndCommand_AndReturnsCreated_WhenValid()
    {
        var options = A.Fake<IOptions<AppSettings>>();
        A.CallTo(() => options.Value).Returns(new AppSettings { MultiTenancy = true, DatabaseEngine = "Postgres" });

        var addTenantCommand = new TestAddTenantCommand();
        var mapper = A.Fake<IMapper>();
        var model = A.Fake<IAddTenantModel>();
        var request = new AddTenant.AddTenantRequest { TenantName = "tenant2" };
        var serviceScopeFactory = A.Fake<IServiceScopeFactory>();
        var tenantsService = A.Fake<ITenantsService>();
        var scope = A.Fake<IServiceScope>();
        var scopedServiceProvider = A.Fake<IServiceProvider>();

        A.CallTo(() => scopedServiceProvider.GetService(typeof(ITenantsService))).Returns(tenantsService);
        A.CallTo(() => mapper.Map<IAddTenantModel>(request)).Returns(model);

        A.CallTo(() => serviceScopeFactory.CreateScope()).Returns(scope);
        A.CallTo(() => scope.ServiceProvider).Returns(scopedServiceProvider);

        var validator = new TestValidator(tenantsService, options);

        var result = await AddTenant.Handle(validator, addTenantCommand, mapper, request, serviceScopeFactory, options);

        result.ShouldNotBeNull();
        result.GetType().Name.ShouldBe("Created");
    }

    private class TestValidator(ITenantsService service, IOptions<AppSettings> options) : AddTenant.Validator(service, options)
    {
        public Task GuardAsync(AddTenant.AddTenantRequest request)
        {
            // Always succeed
            return Task.CompletedTask;
        }
    }

    private class TestAddTenantCommand : AddTenantCommand
    {
        public string ExecutedTenantName { get; private set; }

        public TestAddTenantCommand() : base(A.Fake<FileSystemAppSettingsFileProvider>())
        {
        }

        public override void Execute(IAddTenantModel model)
        {
            ExecutedTenantName = model.TenantName;
        }
    }
}
