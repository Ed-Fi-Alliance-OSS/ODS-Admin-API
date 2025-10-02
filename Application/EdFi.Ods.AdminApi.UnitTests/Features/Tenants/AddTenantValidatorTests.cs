// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Features.Tenants;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using FakeItEasy;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Tenants;

[TestFixture]
public class AddTenantValidatorTests
{
    private AddTenant.Validator _validator;
    private ITenantsService _tenantsService;
    private IOptions<AppSettings> _options;

    [SetUp]
    public void SetUp()
    {
        _tenantsService = A.Fake<ITenantsService>();
        _options = Options.Create(new AppSettings { DatabaseEngine = "SqlServer" });
        _validator = new AddTenant.Validator(_tenantsService, _options);
    }

    [Test]
    public void Should_Have_Error_When_TenantName_Is_Empty()
    {
        var model = new AddTenant.AddTenantRequest { TenantName = string.Empty };
        var result = _validator.Validate(model);
        result.Errors.Any(x => x.PropertyName == nameof(model.TenantName)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_TenantName_Exceeds_Max_Length()
    {
        var model = new AddTenant.AddTenantRequest { TenantName = new string('A', 101) };
        var result = _validator.Validate(model);
        result.Errors.Any(x => x.PropertyName == nameof(model.TenantName)).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Have_Error_When_TenantName_Is_Not_Unique()
    {
        var existingTenants = new List<TenantModel>
        {
            new() { TenantName = "ExistingTenant" }
        };
        A.CallTo(() => _tenantsService.GetTenantsAsync(true)).Returns(Task.FromResult(existingTenants));

        var model = new AddTenant.AddTenantRequest { TenantName = "ExistingTenant" };
        var result = await _validator.ValidateAsync(model);
        result.Errors.Any(x => x.PropertyName == nameof(model.TenantName)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_EdFiAdminConnectionString_Exceeds_Max_Length()
    {
        var model = new AddTenant.AddTenantRequest
        {
            TenantName = "UniqueTenant",
            EdFiAdminConnectionString = new string('C', 501)
        };
        var result = _validator.Validate(model);
        result.Errors.Any(x => x.PropertyName == nameof(model.EdFiAdminConnectionString)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_EdFiAdminConnectionString_Is_Invalid()
    {
        var model = new AddTenant.AddTenantRequest
        {
            TenantName = "UniqueTenant",
            EdFiAdminConnectionString = "invalid-connection-string"
        };
        var result = _validator.Validate(model);
        result.Errors.Any(x => x.PropertyName == nameof(model.EdFiAdminConnectionString)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_EdFiSecurityConnectionString_Exceeds_Max_Length()
    {
        var model = new AddTenant.AddTenantRequest
        {
            TenantName = "UniqueTenant",
            EdFiSecurityConnectionString = new string('C', 501)
        };
        var result = _validator.Validate(model);
        result.Errors.Any(x => x.PropertyName == nameof(model.EdFiSecurityConnectionString)).ShouldBeTrue();
    }

    [Test]
    public void Should_Have_Error_When_EdFiSecurityConnectionString_Is_Invalid()
    {
        var model = new AddTenant.AddTenantRequest
        {
            TenantName = "UniqueTenant",
            EdFiSecurityConnectionString = "invalid-connection-string"
        };

        var result = _validator.Validate(model);
        result.Errors.Any(x => x.PropertyName == nameof(model.EdFiSecurityConnectionString)).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Not_Have_Error_For_Valid_Model()
    {
        // Setup empty tenants list to ensure uniqueness check passes
        A.CallTo(() => _tenantsService.GetTenantsAsync(true)).Returns(Task.FromResult(new List<TenantModel>()));

        var model = new AddTenant.AddTenantRequest
        {
            TenantName = "UniqueTenant",
            EdFiAdminConnectionString = "Server=.;Database=Admin;Trusted_Connection=True;",
            EdFiSecurityConnectionString = "Server=.;Database=Security;Trusted_Connection=True;"
        };

        var result = await _validator.ValidateAsync(model);
        result.IsValid.ShouldBeTrue();
    }
}
