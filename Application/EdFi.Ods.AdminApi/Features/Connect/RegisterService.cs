// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Common.Infrastructure.Security;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Security;
using FluentValidation;
using FluentValidation.Results;
using OpenIddict.Abstractions;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.RegularExpressions;

namespace EdFi.Ods.AdminApi.Features.Connect;

public interface IRegisterService
{
    Task<bool> Handle(RegisterService.RegisterClientRequest request);
}

public class RegisterService : IRegisterService
{
    private readonly IConfiguration _configuration;
    private readonly Validator _validator;
    private readonly IOpenIddictApplicationManager _applicationManager;

    public RegisterService(IConfiguration configuration, Validator validator, IOpenIddictApplicationManager applicationManager)
    {
        _configuration = configuration;
        _validator = validator;
        _applicationManager = applicationManager;
    }

    public async Task<bool> Handle(RegisterClientRequest request)
    {
        if (!await RegistrationIsEnabledOrNecessary())
            return false;

        await _validator.GuardAsync(request);

        var existingApp = await _applicationManager.FindByClientIdAsync(request.ClientId!);
        if (existingApp != null)
            throw new ValidationException(new[] { new ValidationFailure(nameof(request.ClientId), $"ClientId {request.ClientId} already exists") });

        var application = new OpenIddictApplicationDescriptor
        {
            ClientId = request.ClientId,
            ClientSecret = request.ClientSecret,
            DisplayName = request.DisplayName,
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
            },
        };
        foreach (var scopeValue in SecurityConstants.Scopes.AllScopes)
        {
            application.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scopeValue.Scope);
        }

        await _applicationManager.CreateAsync(application);
        return true;
    }

    private async Task<bool> RegistrationIsEnabledOrNecessary()
    {
        var registrationIsEnabled = _configuration.GetValue<bool>("Authentication:AllowRegistration");
        return await Task.FromResult(registrationIsEnabled);
    }

    public class Validator : AbstractValidator<RegisterClientRequest>
    {
        public Validator()
        {
            RuleFor(m => m.ClientId).NotEmpty();
            RuleFor(m => m.ClientSecret)
                .NotEmpty()
                .Matches(new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{32,128}$"))
                .WithMessage(FeatureConstants.ClientSecretValidationMessage);
            RuleFor(m => m.DisplayName).NotEmpty();
        }
    }

    [SwaggerSchema(Title = "RegisterClientRequest")]
    public class RegisterClientRequest
    {
        [SwaggerSchema(Description = "Client id", Nullable = false)]
        public string? ClientId { get; set; }
        [SwaggerSchema(Description = "Client secret", Nullable = false)]
        public string? ClientSecret { get; set; }
        [SwaggerSchema(Description = "Client display name", Nullable = false)]
        public string? DisplayName { get; set; }
    }
}
