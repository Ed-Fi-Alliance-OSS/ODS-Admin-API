// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Linq;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using FluentValidation;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ClaimSets
{
    [TestFixture]
    public class EditClaimSetTests
    {
        [Test]
        public void Handle_WithMismatchedBodyId_ThrowsValidationException()
        {
            var request = new EditClaimSet.EditClaimSetRequest
            {
                Id = 999,
                Name = "Test Claim Set"
            };

            var feature = new EditClaimSet();
            var exception = Should.Throw<ValidationException>(() => feature.Handle(null!, null!, request, 1).GetAwaiter().GetResult());

            exception.Errors.Single(x => x.PropertyName == nameof(request.Id)).ErrorMessage
                .ShouldBe(ErrorMessagesConstants.RequestBodyIdMismatch);
        }
    }
}