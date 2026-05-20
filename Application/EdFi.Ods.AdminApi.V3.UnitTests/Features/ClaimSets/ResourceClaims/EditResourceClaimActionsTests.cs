// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using EdFi.Ods.AdminApi.Common.Constants;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets.ResourceClaims;
using EdFi.Ods.AdminApi.V3.Infrastructure.ClaimSetEditor;
using FluentValidation;
using NUnit.Framework;
using Shouldly;

namespace EdFi.Ods.AdminApi.V3.UnitTests.Features.ClaimSets.ResourceClaims
{
    [TestFixture]
    public class EditResourceClaimActionsTests
    {
        [Test]
        public void HandleEditResourceClaims_WithMismatchedClaimSetId_ThrowsValidationException()
        {
            var request = new EditResourceClaimActions.EditResourceClaimOnClaimSetRequest
            {
                ClaimSetId = 999,
                ResourceClaimId = 2,
                ResourceClaimActions = new List<ResourceClaimAction>()
            };

            var exception = Should.Throw<ValidationException>(() => EditResourceClaimActions.HandleEditResourceClaims(null!, null!, null!, null!, request, 1, 2).GetAwaiter().GetResult());

            exception.Errors.Single(x => x.PropertyName == nameof(request.ClaimSetId)).ErrorMessage
                .ShouldBe(ErrorMessagesConstants.RequestBodyIdMismatch);
        }

        [Test]
        public void HandleEditResourceClaims_WithMismatchedResourceClaimId_ThrowsValidationException()
        {
            var request = new EditResourceClaimActions.EditResourceClaimOnClaimSetRequest
            {
                ClaimSetId = 1,
                ResourceClaimId = 999,
                ResourceClaimActions = new List<ResourceClaimAction>()
            };

            var exception = Should.Throw<ValidationException>(() => EditResourceClaimActions.HandleEditResourceClaims(null!, null!, null!, null!, request, 1, 2).GetAwaiter().GetResult());

            exception.Errors.Single(x => x.PropertyName == nameof(request.ResourceClaimId)).ErrorMessage
                .ShouldBe(ErrorMessagesConstants.RequestBodyIdMismatch);
        }
    }
}