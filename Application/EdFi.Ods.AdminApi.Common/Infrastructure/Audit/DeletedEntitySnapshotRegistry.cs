// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.DataAccess.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.Models;
using EdFi.Security.DataAccess.Models;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Audit;

public class DeletedEntitySnapshotRegistry : IDeletedEntitySnapshotRegistry
{
    private static readonly Dictionary<Type, Func<object, object>> _selectors = new()
    {
        [typeof(ApiClient)] = o =>
        {
            var c = (ApiClient)o;
            return new { c.Key, c.Name, c.IsApproved, c.KeyStatus };
        },
        [typeof(Vendor)] = o =>
        {
            var v = (Vendor)o;
            return new { v.VendorName };
        },
        [typeof(Application)] = o =>
        {
            var a = (Application)o;
            return new { a.ApplicationName, a.ClaimSetName, a.OperationalContextUri };
        },
        [typeof(OdsInstance)] = o =>
        {
            var i = (OdsInstance)o;
            return new { i.Name, i.InstanceType };
        },
        [typeof(OdsInstanceDerivative)] = o =>
        {
            var d = (OdsInstanceDerivative)o;
            return new { d.DerivativeType };
        },
        [typeof(OdsInstanceContext)] = o =>
        {
            var c = (OdsInstanceContext)o;
            return new { c.ContextKey, c.ContextValue };
        },
        [typeof(Profile)] = o =>
        {
            var p = (Profile)o;
            return new { p.ProfileName };
        },
        [typeof(OdsInstanceManage)] = o =>
        {
            var m = (OdsInstanceManage)o;
            return new { m.Name, m.OdsInstanceName, m.Status };
        },
        [typeof(ClaimSet)] = o =>
        {
            var c = (ClaimSet)o;
            return new { c.ClaimSetName, c.IsEdfiPreset, c.ForApplicationUseOnly };
        },
        [typeof(ClaimSetResourceClaimAction)] = o =>
        {
            var a = (ClaimSetResourceClaimAction)o;
            return new { a.ClaimSetId, a.ResourceClaimId, a.ActionId };
        }
    };

    public object? Project(object entity)
    {
        return _selectors.TryGetValue(entity.GetType(), out var selector)
            ? selector(entity)
            : null;
    }
}
