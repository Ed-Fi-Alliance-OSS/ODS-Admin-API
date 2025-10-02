// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Threading.Tasks;
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
public class DeleteTenantTests
{
    private ITenantsService _tenantsService = null!;
    private DeleteTenantCommand _deleteTenantCommand = null!;
    private IServiceScopeFactory _serviceScopeFactory = null!;
    private IOptions<AppSettings> _options = null!;

    [SetUp]
    public void SetUp()
    {
        _tenantsService = A.Fake<ITenantsService>();
        _deleteTenantCommand = A.Fake<DeleteTenantCommand>();
        _serviceScopeFactory = A.Fake<IServiceScopeFactory>();
        _options = Options.Create(new AppSettings { MultiTenancy = true });
    }

    [Test]
    public async Task Handle_ShouldReturnBadRequest_WhenMultiTenancyIsDisabled()
    {
        // Arrange
        var options = Options.Create(new AppSettings { MultiTenancy = false });

        // Act
        var result = await DeleteTenant.Handle(
            _tenantsService,
            _deleteTenantCommand,
            _serviceScopeFactory,
            options,
            "tenant1");

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
    public async Task Handle_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        // Arrange
        A.CallTo(() => _tenantsService.GetTenantByTenantIdAsync(A<string>._)).Returns(Task.FromResult<TenantModel>(null));

        // Act
        var result = await DeleteTenant.Handle(
            _tenantsService,
            _deleteTenantCommand,
            _serviceScopeFactory,
            _options,
            "tenant1");

        // Assert
        var notFound = result as IResult;
        notFound.ShouldNotBeNull();
        var httpResult = notFound as Microsoft.AspNetCore.Http.HttpResults.NotFound;
        httpResult.ShouldNotBeNull();
    }

    [Test]
    public async Task Handle_ShouldDeleteTenantAndReturnOk_WhenTenantExists()
    {
        // Arrange
        var tenant = new TenantModel { TenantName = "tenant1" };
        A.CallTo(() => _tenantsService.GetTenantByTenantIdAsync("tenant1")).Returns(Task.FromResult<TenantModel>(tenant));

        var scope = A.Fake<IServiceScope>();
        var scopedServiceProvider = A.Fake<IServiceProvider>();
        var scopedTenantsService = A.Fake<ITenantsService>();

        A.CallTo(() => _serviceScopeFactory.CreateScope()).Returns(scope);
        A.CallTo(() => scope.ServiceProvider).Returns(scopedServiceProvider);
        A.CallTo(() => scopedServiceProvider.GetService(typeof(ITenantsService))).Returns(scopedTenantsService);
        A.CallTo(() => scopedTenantsService.InitializeTenantsAsync()).Returns(Task.CompletedTask);

        // Use a test double for DeleteTenantCommand
        var testDeleteTenantCommand = new TestDeleteTenantCommand();

        // Act
        var result = await DeleteTenant.Handle(
            _tenantsService,
            testDeleteTenantCommand,
            _serviceScopeFactory,
            _options,
            "tenant1");

        // Assert
        testDeleteTenantCommand.ExecutedTenantName.ShouldBe("tenant1");
        A.CallTo(() => scopedTenantsService.InitializeTenantsAsync()).MustHaveHappened();

        var okResult = result as IResult;
        okResult.ShouldNotBeNull();
        var httpResult = okResult as Microsoft.AspNetCore.Http.HttpResults.Ok<object>;
        httpResult.ShouldNotBeNull();
    }

    [Test]
    public void Handle_ShouldThrow_WhenGetTenantByTenantIdAsyncThrows()
    {
        // Arrange
        A.CallTo(() => _tenantsService.GetTenantByTenantIdAsync(A<string>._)).Throws<InvalidOperationException>();

        // Act & Assert
        Should.ThrowAsync<InvalidOperationException>(async () =>
            await DeleteTenant.Handle(
                _tenantsService,
                _deleteTenantCommand,
                _serviceScopeFactory,
                _options,
                "tenant1"));
    }

    private class TestDeleteTenantCommand : DeleteTenantCommand
    {
        public string ExecutedTenantName { get; private set; }

        public TestDeleteTenantCommand() : base(A.Fake<FileSystemAppSettingsFileProvider>())
        {
        }

        public override void Execute(string tenantName)
        {
            ExecutedTenantName = tenantName;
        }
    }
}

