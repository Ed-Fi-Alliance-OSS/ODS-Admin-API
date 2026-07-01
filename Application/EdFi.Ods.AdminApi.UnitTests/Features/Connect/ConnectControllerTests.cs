// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using EdFi.Ods.AdminApi.Features.Connect;
using FakeItEasy;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.UnitTests.Features.Connect;

[TestFixture]
public class ConnectControllerTests
{
    [Test]
    public async Task Register_WhenRegistrationSucceeds_ReturnsOkWithClientMessage()
    {
        var tokenService = A.Fake<ITokenService>();
        var registerService = A.Fake<IRegisterService>();
        var request = new RegisterService.RegisterClientRequest { ClientId = "client-id" };
        A.CallTo(() => registerService.Handle(request)).Returns(true);
        var controller = new ConnectController(tokenService, registerService);

        var result = await controller.Register(request);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        ok.Value.ShouldNotBeNull();
        ok.Value.GetType().GetProperty("Title")!.GetValue(ok.Value).ShouldBe("Registered client client-id successfully.");
        ok.Value.GetType().GetProperty("Status")!.GetValue(ok.Value).ShouldBe(200);
    }

    [Test]
    public async Task Register_WhenRegistrationIsRejected_ReturnsForbid()
    {
        var tokenService = A.Fake<ITokenService>();
        var registerService = A.Fake<IRegisterService>();
        var request = new RegisterService.RegisterClientRequest { ClientId = "client-id" };
        A.CallTo(() => registerService.Handle(request)).Returns(false);
        var controller = new ConnectController(tokenService, registerService);

        var result = await controller.Register(request);

        result.ShouldBeOfType<ForbidResult>();
    }
}

[TestFixture]
public class RegisterServiceValidatorTests
{
    private RegisterService.Validator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new RegisterService.Validator();
    }

    [Test]
    public void Validate_WhenRequestIsValid_ReturnsNoErrors()
    {
        var request = ValidRequest();

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void Validate_WhenClientSecretDoesNotMeetComplexity_ReturnsClientSecretError()
    {
        var request = ValidRequest();
        request.ClientSecret = "too-simple";

        var result = _validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.ClientSecret)).ShouldBeTrue();
    }

    [Test]
    public void Validate_WhenClientIdIsEmpty_ReturnsClientIdError()
    {
        var request = ValidRequest();
        request.ClientId = string.Empty;

        var result = _validator.Validate(request);

        result.Errors.Any(x => x.PropertyName == nameof(request.ClientId)).ShouldBeTrue();
    }

    private static RegisterService.RegisterClientRequest ValidRequest()
    {
        return new RegisterService.RegisterClientRequest
        {
            ClientId = "client-id",
            ClientSecret = "ValidClientSecret123456789012345!",
            DisplayName = "Client Display"
        };
    }
}
