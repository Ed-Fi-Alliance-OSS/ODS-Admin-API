// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Features;
using EdFi.Ods.AdminApi.V3.Infrastructure.ErrorHandling;

namespace EdFi.Ods.AdminApi.V3.Infrastructure.Database.Commands;

public sealed record ClientEducationOrganizationScope(bool Inherits, IReadOnlyList<long> EducationOrganizationIds);

public static class ClientEducationOrganizationScopeResolver
{
    private const string IdsProperty = "educationOrganizationIds";
    private const string InheritsProperty = "inheritsApplicationEducationOrganizations";

    // current is null on POST; isMove is true on a PUT whose applicationId differs from the client's.
    public static ClientEducationOrganizationScope Resolve(
        IEnumerable<long>? requestedIds,
        bool? requestedInherits,
        IReadOnlyCollection<long> applicationIds,
        ClientEducationOrganizationScope? current,
        bool isMove)
    {
        var inheriting = new ClientEducationOrganizationScope(true, applicationIds.Distinct().ToList());

        if (requestedIds is not null)
        {
            var ids = requestedIds.Distinct().ToList();

            if (ids.Count == 0)
            {
                throw new BadRequestDataException(IdsProperty, FeatureConstants.ApiClientEdOrgIdsEmptyMessage);
            }

            var notOnApplication = ids.Where(id => !applicationIds.Contains(id)).ToList();

            if (notOnApplication.Count > 0)
            {
                throw new BadRequestDataException(
                    IdsProperty,
                    string.Format(FeatureConstants.ApiClientEdOrgIdsNotOnApplicationMessage, string.Join(", ", notOnApplication)));
            }

            if (requestedInherits == true)
            {
                if (!ids.ToHashSet().SetEquals(applicationIds))
                {
                    throw new BadRequestDataException(
                        InheritsProperty,
                        FeatureConstants.ApiClientInheritsTrueListMismatchMessage);
                }

                return inheriting;
            }

            return new ClientEducationOrganizationScope(false, ids);
        }

        if (requestedInherits == true)
        {
            return inheriting;
        }

        if (requestedInherits == false)
        {
            throw new BadRequestDataException(IdsProperty, FeatureConstants.ApiClientInheritsFalseNoListMessage);
        }

        if (current is null)
        {
            return inheriting;
        }

        if (current.Inherits)
        {
            return inheriting;
        }

        if (!isMove)
        {
            return current;
        }

        var missing = current.EducationOrganizationIds.Where(id => !applicationIds.Contains(id)).Distinct().ToList();

        if (missing.Count > 0)
        {
            throw new ConflictException(
                string.Format(FeatureConstants.ApiClientMoveScopeConflictMessage, string.Join(", ", missing)),
                AdminApiProblemTypes.ConflictEducationOrganizationScope);
        }

        return current;
    }
}
