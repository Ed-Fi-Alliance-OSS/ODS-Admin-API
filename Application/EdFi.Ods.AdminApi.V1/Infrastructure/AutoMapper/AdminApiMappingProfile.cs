// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.V1.Features.Applications;
using EdFi.Ods.AdminApi.V1.Features.ClaimSets;
using EdFi.Ods.AdminApi.V1.Features.OdsInstances;
using EdFi.Ods.AdminApi.V1.Features.Vendors;
using EdFi.Ods.AdminApi.V1.Infrastructure.Database.Commands;
using EdFi.Ods.AdminApi.V1.Infrastructure.Helpers;
using EdFi.Ods.AdminApi.V1.Infrastructure.Services.ClaimSetEditor;
using Profile = AutoMapper.Profile;

namespace EdFi.Ods.AdminApi.V1.Infrastructure;

public class AdminApiMappingProfile : Profile
{
    public AdminApiMappingProfile()
    {

        CreateMap<Vendor, EditVendor.Request>()
            .ForMember(dst => dst.Company, opt => opt.MapFrom(src => src.VendorName))
            .ForMember(dst => dst.ContactName, opt => opt.MapFrom(src => src.ContactName()))
            .ForMember(dst => dst.ContactEmailAddress, opt => opt.MapFrom(src => src.ContactEmail()))
            .ForMember(dst => dst.NamespacePrefixes, opt => opt.MapFrom(src => src.VendorNamespacePrefixes.ToCommaSeparated()));

        CreateMap<Vendor, VendorModel>()
            .ForMember(dst => dst.Company, opt => opt.MapFrom(src => src.VendorName))
            .ForMember(dst => dst.ContactName, opt => opt.MapFrom(src => src.ContactName()))
            .ForMember(dst => dst.ContactEmailAddress, opt => opt.MapFrom(src => src.ContactEmail()))
            .ForMember(dst => dst.NamespacePrefixes, opt => opt.MapFrom(src => src.VendorNamespacePrefixes.ToCommaSeparated()));

        CreateMap<Application, ApplicationModel>()
            .ForMember(dst => dst.EducationOrganizationIds, opt => opt.MapFrom(src => src.EducationOrganizationIds()))
            .ForMember(dst => dst.ProfileName, opt => opt.MapFrom(src => src.ProfileName()))
            .ForMember(dst => dst.OdsInstanceId, opt => opt.MapFrom(src => src.OdsInstance.OdsInstanceId))
            .ForMember(dst => dst.OdsInstanceName, opt => opt.MapFrom(src => src.OdsInstanceName()))
            .ForMember(dst => dst.VendorId, opt => opt.MapFrom(src => src.VendorId()))
            .ForMember(dst => dst.Profiles, opt => opt.MapFrom(src => src.Profiles()));
            

        CreateMap<AddApplicationResult, ApplicationResult>()
            .ForMember(dst => dst.ApplicationId, opt => opt.MapFrom(src => src.ApplicationId))
            .ForMember(dst => dst.Key, opt => opt.MapFrom(src => src.Key))
            .ForMember(dst => dst.Secret, opt => opt.MapFrom(src => src.Secret));

        CreateMap<RegenerateApiClientSecretResult, ApplicationResult>()
            .ForMember(dst => dst.ApplicationId, opt => opt.MapFrom(src => src.Application.ApplicationId))
            .ForMember(dst => dst.Key, opt => opt.MapFrom(src => src.Key))
            .ForMember(dst => dst.Secret, opt => opt.MapFrom(src => src.Secret));

        CreateMap<OdsInstance, OdsInstanceModel>()
            //.ForMember(dst => dst.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dst => dst.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dst => dst.InstanceType, opt => opt.MapFrom(src => src.InstanceType))
            .ForMember(dst => dst.Version, opt => opt.MapFrom(src => src.Version))
            .ForMember(dst => dst.IsExtended, opt => opt.MapFrom(src => src.IsExtended))
            .ForMember(dst => dst.Status, opt => opt.MapFrom(src => src.Status));

        CreateMap<ClaimSetEditor.ClaimSet, ClaimSetDetailsModel>()
            .ForMember(dst => dst.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dst => dst.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dst => dst.IsSystemReserved, opt => opt.MapFrom(src => !src.IsEditable));

        CreateMap<ClaimSetEditor.ClaimSet, ClaimSetModel>()
            .ForMember(dst => dst.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dst => dst.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dst => dst.IsSystemReserved, opt => opt.MapFrom(src => !src.IsEditable));

        CreateMap<ClaimSetEditor.ResourceClaim, ResourceClaimModel>()
            .ForMember(dst => dst.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dst => dst.Read, opt => opt.MapFrom(src => src.Read))
            .ForMember(dst => dst.Update, opt => opt.MapFrom(src => src.Update))
            .ForMember(dst => dst.Create, opt => opt.MapFrom(src => src.Create))
            .ForMember(dst => dst.Delete, opt => opt.MapFrom(src => src.Delete))
            .ForMember(dst => dst.ReadChanges, opt => opt.MapFrom(src => src.ReadChanges))
            .ForMember(dst => dst.AuthStrategyOverridesForCRUD, opt => opt.MapFrom(src => src.AuthStrategyOverridesForCRUD))
            .ForMember(dst => dst.DefaultAuthStrategiesForCRUD, opt => opt.MapFrom(src => src.DefaultAuthStrategiesForCRUD))
            .ForMember(dst => dst.Children, opt => opt.MapFrom(src => src.Children));

        CreateMap<EdFi.Ods.AdminApi.V1.Infrastructure.ClaimSetEditor.AuthorizationStrategy, AuthorizationStrategyModel>()
            .ForMember(dst => dst.AuthStrategyId, opt => opt.MapFrom(src => src.AuthStrategyId))
            .ForMember(dst => dst.AuthStrategyName, opt => opt.MapFrom(src => src.AuthStrategyName))
            .ForMember(dst => dst.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
            .ForMember(dst => dst.IsInheritedFromParent, opt => opt.MapFrom(src => src.IsInheritedFromParent));

        CreateMap<AuthorizationStrategyModel, EdFi.Ods.AdminApi.V1.Infrastructure.ClaimSetEditor.AuthorizationStrategy>()
            .ForMember(dst => dst.AuthStrategyId, opt => opt.MapFrom(src => src.AuthStrategyId))
            .ForMember(dst => dst.AuthStrategyName, opt => opt.MapFrom(src => src.AuthStrategyName))
            .ForMember(dst => dst.DisplayName, opt => opt.MapFrom(src => src.DisplayName))
            .ForMember(dst => dst.IsInheritedFromParent, opt => opt.MapFrom(src => src.IsInheritedFromParent));

        CreateMap<EdFi.Ods.AdminApi.V1.Security.DataAccess.Models.AuthorizationStrategy, EdFi.Ods.AdminApi.V1.Infrastructure.ClaimSetEditor.AuthorizationStrategy>()
            .ForMember(dst => dst.AuthStrategyName, opt => opt.MapFrom(src => src.AuthorizationStrategyName))
            .ForMember(dst => dst.AuthStrategyId, opt => opt.MapFrom(src => src.AuthorizationStrategyId))
            .ForMember(dst => dst.IsInheritedFromParent, opt => opt.Ignore());

        CreateMap<ResourceClaimModel, EdFi.Ods.AdminApi.V1.Infrastructure.ClaimSetEditor.ResourceClaim>()
            .ForMember(dst => dst.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dst => dst.Read, opt => opt.MapFrom(src => src.Read))
            .ForMember(dst => dst.Update, opt => opt.MapFrom(src => src.Update))
            .ForMember(dst => dst.Create, opt => opt.MapFrom(src => src.Create))
            .ForMember(dst => dst.Delete, opt => opt.MapFrom(src => src.Delete))
            .ForMember(dst => dst.ReadChanges, opt => opt.MapFrom(src => src.ReadChanges))
            .ForMember(dst => dst.AuthStrategyOverridesForCRUD, opt => opt.MapFrom(src => src.AuthStrategyOverridesForCRUD))
            .ForMember(dst => dst.DefaultAuthStrategiesForCRUD, opt => opt.MapFrom(src => src.DefaultAuthStrategiesForCRUD))
            .ForMember(dst => dst.Children, opt => opt.MapFrom(src => src.Children));

        CreateMap<AuthorizationStrategiesModel, ClaimSetResourceClaimActionAuthStrategies>()
        .ForMember(dst => dst.AuthorizationStrategies, opt => opt.MapFrom(src => src.AuthorizationStrategies)).ReverseMap();
           ;

        CreateMap<RequestResourceClaimModel, EdFi.Ods.AdminApi.V1.Infrastructure.ClaimSetEditor.ResourceClaim>()
           .ForMember(dst => dst.Name, opt => opt.MapFrom(src => src.Name))
           .ForMember(dst => dst.Read, opt => opt.MapFrom(src => src.Read))
           .ForMember(dst => dst.Update, opt => opt.MapFrom(src => src.Update))
           .ForMember(dst => dst.Create, opt => opt.MapFrom(src => src.Create))
           .ForMember(dst => dst.Delete, opt => opt.MapFrom(src => src.Delete))
           .ForMember(dst => dst.ReadChanges, opt => opt.MapFrom(src => src.ReadChanges))
           .ForMember(dst => dst.AuthStrategyOverridesForCRUD, opt => opt.MapFrom(src => src.AuthStrategyOverridesForCRUD))
           .ForMember(dst => dst.Children, opt => opt.MapFrom(src => src.Children));

        CreateMap<RequestResourceClaimModel, ChildrenRequestResourceClaimModel>()
            .ForMember(dst => dst.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dst => dst.Read, opt => opt.MapFrom(src => src.Read))
            .ForMember(dst => dst.Update, opt => opt.MapFrom(src => src.Update))
            .ForMember(dst => dst.Create, opt => opt.MapFrom(src => src.Create))
            .ForMember(dst => dst.Delete, opt => opt.MapFrom(src => src.Delete))
            .ForMember(dst => dst.ReadChanges, opt => opt.MapFrom(src => src.ReadChanges))
            .ForMember(dst => dst.AuthStrategyOverridesForCRUD, opt => opt.MapFrom(src => src.AuthStrategyOverridesForCRUD))
            .ForMember(dst => dst.Children, opt => opt.MapFrom(src => src.Children));
    }
}
