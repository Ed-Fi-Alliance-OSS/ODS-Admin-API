// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using AutoMapper;
using EdFi.Ods.AdminApi.Common.Features;
using EdFi.Ods.AdminApi.Common.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure;
using EdFi.Ods.AdminApi.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.Infrastructure.Database.Queries;
using EdFi.Ods.AdminApi.Infrastructure.Documentation;
using FluentValidation;
using Swashbuckle.AspNetCore.Annotations;

namespace EdFi.Ods.AdminApi.Features.Profiles;

public class AddProfile : IFeature
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        AdminApiEndpointBuilder
           .MapPost(endpoints, "/profiles", Handle)
           .WithDefaultSummaryAndDescription()
           .WithRouteOptions(b => b.WithResponseCode(201))
           .BuildForVersions(AdminApiVersions.V2);
    }

    [ProfileRequestExample]
    public async Task<IResult> Handle(Validator validator, IAddProfileCommand addProfileCommand, IMapper mapper, AddProfileRequest request)
    {
        await validator.GuardAsync(request);
        var addedProfile = addProfileCommand.Execute(request);
        return Results.Created($"/profiles/{addedProfile.ProfileId}", null);
    }

    [SwaggerSchema(Title = "AddProfileRequest")]
    public class AddProfileRequest : IAddProfileModel
    {
        [SwaggerSchema(Description = FeatureConstants.ProfileName, Nullable = false)]
        public string? Name { get; set; }

        [SwaggerSchema(Description = FeatureConstants.ProfileDefinition, Nullable = false)]
        public string? Definition { get; set; }
    }

    public class Validator : AbstractValidator<AddProfileRequest>
    {
        private readonly IGetProfilesQuery _getProfilesQuery;

        public Validator(IGetProfilesQuery getProfilesQuery)
        {
            _getProfilesQuery = getProfilesQuery;

            RuleFor(m => m.Name).NotEmpty();

            RuleFor(m => m.Name)
                .Must(BeAUniqueName)
                .WithMessage(FeatureConstants.ProfileAlreadyExistsMessage);

            RuleFor(m => m.Definition).NotEmpty();

            RuleFor(m => m).Custom((profile, context) =>
            {
                if (!string.IsNullOrEmpty(profile.Definition))
                {
                    var validator = new ProfileValidator();
                    validator.Validate(profile.Name!, profile.Definition, context);
                }
            });
        }

        private bool BeAUniqueName(string? profileName)
        {
            return _getProfilesQuery.Execute().TrueForAll(x => x.ProfileName != profileName);
        }
    }
}
