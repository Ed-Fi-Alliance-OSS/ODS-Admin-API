// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Net;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure.Security;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using FluentValidation;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.Connect;

[AllowAnonymous]
[SwaggerResponse(400, FeatureCommonConstants.BadRequestResponseDescription)]
[SwaggerResponse(500, FeatureCommonConstants.InternalServerErrorResponseDescription)]
[Route(SecurityConstants.ConnectRoute)]
public class ConnectController(ITokenService tokenService, IRegisterService registerService) : Controller
{
    private readonly ITokenService _tokenService = tokenService;
    private readonly IRegisterService _registerService = registerService;

    [HttpPost(SecurityConstants.RegisterActionName)]
    [Consumes("application/x-www-form-urlencoded"), Produces("application/json")]
    [SwaggerOperation("Registers new client", "Registers new client")]
    [SwaggerResponse(200, "Application registered successfully.")]
    public async Task<IActionResult> Register([FromForm] RegisterService.RegisterClientRequest request)
    {
        if (await _registerService.Handle(request))
        {
            return Ok(new { Title = $"Registered client {request.ClientId} successfully.", Status = 200 });
        }
        return new ForbidResult();
    }

    [HttpPost(SecurityConstants.TokenActionName)]
    [Consumes("application/x-www-form-urlencoded"), Produces("application/json")]
    [SwaggerOperation("Retrieves bearer token", "\nTo authenticate Swagger requests, execute using \"Authorize\" above, not \"Try It Out\" here.")]
    [SwaggerResponse(200, "Sign-in successful.")]
    [SwaggerResponse(400, "Bad request, such as invalid scope.")]
    public async Task<ActionResult> Token()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ?? throw new ValidationException("Failed to parse token request");

        try
        {
            var principal = await _tokenService.Handle(request);
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        catch (AdminApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            // Return a 400 Bad Request response for invalid scopes with proper content type
            Response.ContentType = "application/problem+json";
            return BadRequest(new
            {
                error = "invalid_scope",
                error_description = ex.Message,
                status = 400,
                title = "Invalid Scope"
            });
        }
    }
}
