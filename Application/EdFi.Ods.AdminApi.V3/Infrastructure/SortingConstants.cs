// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.V3.Features.Actions;
using EdFi.Ods.AdminApi.V3.Features.Applications;
using EdFi.Ods.AdminApi.V3.Features.AuthorizationStrategies;
using EdFi.Ods.AdminApi.V3.Features.ClaimSets;
using EdFi.Ods.AdminApi.V3.Features.OdsInstanceContext;
using EdFi.Ods.AdminApi.V3.Features.OdsInstanceDerivative;
using EdFi.Ods.AdminApi.V3.Features.OdsInstances;
using EdFi.Ods.AdminApi.V3.Features.Vendors;

namespace EdFi.Ods.AdminApi.V3.Infrastructure;

public static class SortingColumns
{
    /// Default columns to filter for each entity:
    public const string DefaultNameColumn = "Name";
    public const string DefaultIdColumn = "Id";
    public const string ApplicationNameColumn = nameof(ApplicationModel.ApplicationName);
    public const string ApplicationClaimSetNameColumn = nameof(ApplicationModel.ClaimSetName);
    public const string ActionUriColumn = nameof(ActionModel.Uri);
    public const string AuthorizationStrategyDisplayNameColumn = nameof(AuthorizationStrategyModel.DisplayName);
    public const string OdsInstanceContextKeyColumn = nameof(OdsInstanceContextModel.ContextKey);
    public const string OdsInstanceContextValueColumn = nameof(OdsInstanceContextModel.ContextValue);
    public const string OdsInstanceDerivativeTypeColumn = nameof(OdsInstanceDerivativeModel.DerivativeType);
    public const string OdsInstanceDerivativeOdsInstanceIdColumn = nameof(OdsInstanceDerivativeModel.OdsInstanceId);
    public const string OdsInstanceInstanceTypeColumn = nameof(OdsInstanceModel.InstanceType);
    public const string ResourceClaimParentNameColumn = nameof(ResourceClaimModel.ParentName);
    public const string ResourceClaimParentIdColumn = nameof(ResourceClaimModel.ParentId);
    public const string VendorCompanyColumn = nameof(VendorModel.Company);
    public const string VendorContactNameColumn = nameof(VendorModel.ContactName);
    public const string VendorContactEmailColumn = nameof(VendorModel.ContactEmailAddress);
    public const string VendorNamespacePrefixesColumn = nameof(VendorModel.NamespacePrefixes);




}
