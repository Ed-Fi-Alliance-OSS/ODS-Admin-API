// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using EdFi.Ods.AdminApi.Common.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.Common.Settings;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Services.Tenants;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.Tenants;

public class AddTenant : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
            .MapPost(endpoints, "/tenants", Handle)
            .WithDefaultSummaryAndDescription()
            .WithRouteOptions(b => b.WithResponseCode(201))
            .BuildForVersions(AdminApiVersions.V2);
    }

    public static async Task<IResult> Handle(
        Validator validator,
        AddTenantCommand addTenantCommand,
        IMapper mapper,
        AddTenantRequest request,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<AppSettings> options)
    {
        if (!options.Value.MultiTenancy)
            return Results.BadRequest(new { message = "Not multitenant environment." });

        await validator.GuardAsync(request);

        var model = mapper.Map<IAddTenantModel>(request);
        addTenantCommand.Execute(model);

        await InitializeTenantsAsync(serviceScopeFactory);

        return Results.Created($"/tenants/{request.TenantName}", null);
    }

    [SwaggerSchema(Title = "AddTenantRequest")]
    public class AddTenantRequest : IAddTenantModel
    {
        [SwaggerSchema(Description = "The unique name of the tenant.", Nullable = false)]
        public string TenantName { get; set; } = string.Empty;

        [SwaggerSchema(Description = "The connection string for EdFi_Security.", Nullable = true)]
        public string? EdFiSecurityConnectionString { get; set; }

        [SwaggerSchema(Description = "The connection string for EdFi_Admin.", Nullable = true)]
        public string? EdFiAdminConnectionString { get; set; }
    }

    public class Validator : AbstractValidator<AddTenantRequest>
    {
        private readonly ITenantsService _tenantsService;
        private readonly string _databaseEngine;

        public Validator([FromServices] ITenantsService tenantsService, IOptions<AppSettings> options)
        {
            _tenantsService = tenantsService;
            _databaseEngine = options.Value.DatabaseEngine ?? throw new NotFoundException<string>("AppSettings", "DatabaseEngine");

            RuleFor(x => x.TenantName)
                .NotEmpty()
                .MaximumLength(100)
                .Must(BeAUniqueName)
                .WithMessage(FeatureConstants.TenantAlreadyExistsMessage);

            RuleFor(m => m.EdFiAdminConnectionString)
                .MaximumLength(500)
                .Must(BeAValidConnectionString)
                .WithMessage(FeatureConstants.TenantConnectionStringInvalid)
                .When(m => !string.IsNullOrEmpty(m.EdFiAdminConnectionString));

            RuleFor(m => m.EdFiSecurityConnectionString)
                .MaximumLength(500)
                .Must(BeAValidConnectionString)
                .WithMessage(FeatureConstants.TenantConnectionStringInvalid)
                .When(m => !string.IsNullOrEmpty(m.EdFiSecurityConnectionString));
        }

        private bool BeAUniqueName(string name)
        {
            var tenants = _tenantsService.GetTenantsAsync(true).Result;
            return tenants.TrueForAll(x => x.TenantName != name);
        }

        private bool BeAValidConnectionString(string? connectionString)
        {
            return ConnectionStringHelper.ValidateConnectionString(_databaseEngine, connectionString);
        }
    }

    private static async Task InitializeTenantsAsync(IServiceScopeFactory serviceScopeFactory)
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();

        ITenantsService scopedProcessingService =
            scope.ServiceProvider.GetRequiredService<ITenantsService>();

        await scopedProcessingService.InitializeTenantsAsync();
    }
}
