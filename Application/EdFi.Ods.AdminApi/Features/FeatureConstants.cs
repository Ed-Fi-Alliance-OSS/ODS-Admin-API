// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Ods.AdminApi.Features;

public static class FeatureConstants
{
    public const string VendorIdDescription = "Vendor/ company id";
    public const string VendorNameDescription = "Vendor/ company name";
    public const string VendorNamespaceDescription = "Namespace prefix for the vendor. Multiple namespace prefixes can be provided as comma separated list if required.";
    public const string VendorContactDescription = "Vendor contact name";
    public const string VendorContactEmailDescription = "Vendor contact email id";
    public const string ApplicationIdDescription = "Application id";
    public const string ApplicationNameDescription = "Application name";
    public const string ClaimSetIdDescription = "Claim set id";
    public const string ClaimSetNameDescription = "Claim set name";
    public const string ProfileIdDescription = "Profile id";
    public const string EducationOrganizationIdsDescription = "Education organization ids";
    public const string Enable = "Indicates whether the ApiClient's credetials is enabled. Defaults to true if not provided.";
    public const string ResourceClaimIdDescription = "Resource Claim Id";
    public const string ResourceClaimNameDescription = "Resource Claim Name";
    public const string ResourceClaimsDescription = "Resource Claims";
    public const string ApplicationNameLengthValidationMessage = "The Application Name {ApplicationName} would be too long for Admin App to set up necessary Application records." +
                        " Consider shortening the name by {ExtraCharactersInName} character(s).";
    public const string ClaimSetNameValidationMessage = "Please provide a valid claim set name.";
    public const string EdOrgIdsValidationMessage = "Please provide at least one education organization id.";
    public const string VendorIdValidationMessage = "Please provide valid vendor id.";
    public const string ClaimSetAlreadyExistsMessage = "A claim set with this name already exists in the database. Please enter a unique name.";
    public const string TenantAlreadyExistsMessage = "A tenant with this name already exists. Please enter a unique name.";
    public const string ClaimSetNameMaxLengthMessage = "The claim set name must be less than 255 characters.";
    public const string ClaimSetNotFound = "No such claim set exists in the database.";
    public const string InvalidResourceClaimActions = "Please provide a valid resourceClaimActions object.";
    public const string ResourceClaimOneActionNotSet = "A resource must have at least one action associated with it to be added.";
    public const string OdsInstanceIdsDescription = "List of ODS instance id";
    public const string OdsInstanceIdsValidationMessage = "Please provide at least one ods instance id.";
    public const string OdsInstanceIdValidationMessage = "Please provide valid ods instance id. The id {OdsInstanceId} does not exist.";
    public const string ClaimSetIdToCopy = "ClaimSet id to copy";
    public const string ProfileName = "Profile name";
    public const string ProfileAlreadyExistsMessage = "A Profile with this name already exists in the database. Please enter a unique name.";
    public const string AnotherProfileAlreadyExistsMessage = "Another Profile with this name already exists in the database. Please enter a unique name.";
    public const string ProfileDefinition = "Profile definition";
    public const string OdsInstanceName = "Ods Instance name";
    public const string OdsInstanceInstanceType = "Ods Instance type";
    public const string OdsInstanceConnectionString = "Ods Instance connection string";
    public const string OdsInstanceAlreadyExistsMessage = "An Ods instance with this name already exists in the database. Please enter a unique name.";
    public const string OdsInstanceCantBeDeletedMessage = "There are some {Table} associated to this OdsInstance. Can not be deleted.";
    public const string OdsInstanceDerivativeCombinedKeyMustBeUnique = "The combined key ODS instance id and derivative type must be unique.";
    public const string OdsInstanceContextCombinedKeyMustBeUnique = "The combined key ODS instance id and context key must be unique.";
    public const string OdsInstanceConnectionStringInvalid = "The connection string is not valid.";
    public const string TenantConnectionStringInvalid = "The connection string is not valid.";
    public const string OdsInstanceDerivativeIdDescription = "ODS instance derivative id.";
    public const string OdsInstanceDerivativeOdsInstanceIdDescription = "ODS instance derivative ODS instance id.";
    public const string OdsInstanceDerivativeDerivativeTypeDescription = "derivative type.";
    public const string OdsInstanceDerivativeConnectionStringDescription = "connection string.";
    public const string OdsInstanceDerivativeDerivativeTypeNotValid = "The value for the Derivative type is not allowed. The only accepted values are: 'ReadReplica' or 'Snapshot'.";
    public const string OdsInstanceContextIdDescription = "ODS instance context id.";
    public const string OdsInstanceContextOdsInstanceIdDescription = "ODS instance context ODS instance id.";
    public const string OdsInstanceContextContextKeyDescription = "context key.";
    public const string OdsInstanceContextContextValueDescription = "context value.";
    public const string ClientSecretValidationMessage = "ClientSecret must contain at least one lowercase letter, one uppercase letter, one number, and one special character, and must be 32 to 128 characters long.";
    public const string ActionIdDescription = "Action id";
    public const string ActionNameDescription = "Action name";

    public const string ApiClientNameDescription = "Api client name";
    public const string ApiClientIsApprovedDescription = "Is approved";
    public const string ApiClientApplicationIdDescription = "Application id";
    public const string ApiClientNameLengthValidationMessage = "The Api Client Name {Name} would be too long for Admin App to set up necessary ApiClient records." +
                        " Consider shortening the name by {ExtraCharactersInName} character(s).";
}
