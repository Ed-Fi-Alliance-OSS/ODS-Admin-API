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
            // ApplicationName is only unique within a vendor (see AddApplicationCommand's
            // VendorId+ApplicationName uniqueness check), so the vendor name is required to
            // make this snapshot's key actually identify the deleted object. Requires the
            // caller to have loaded Vendor (see DeleteApplicationCommand's .Include(a => a.Vendor)).
            var a = (Application)o;
            return new { a.ApplicationName, a.ClaimSetName, a.OperationalContextUri, VendorName = a.Vendor.VendorName };
        },
        [typeof(OdsInstance)] = o =>
        {
            var i = (OdsInstance)o;
            return new { i.Name, i.InstanceType };
        },
        [typeof(OdsInstanceDerivative)] = o =>
        {
            // DerivativeType is only unique within an OdsInstance (see AddOdsInstanceDerivative's
            // OdsInstanceId+DerivativeType uniqueness check). Requires the caller to have loaded
            // OdsInstance (see DeleteOdsInstanceDerivativeCommand's .Include(d => d.OdsInstance)).
            var d = (OdsInstanceDerivative)o;
            return new { d.DerivativeType, OdsInstanceName = d.OdsInstance.Name };
        },
        [typeof(OdsInstanceContext)] = o =>
        {
            // ContextKey is only unique within an OdsInstance (see AddOdsInstanceContext's
            // OdsInstanceId+ContextKey uniqueness check). Requires the caller to have loaded
            // OdsInstance (see DeleteOdsInstanceContextCommand's .Include(c => c.OdsInstance)).
            var c = (OdsInstanceContext)o;
            return new { c.ContextKey, c.ContextValue, OdsInstanceName = c.OdsInstance.Name };
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
        [typeof(DeletedClaimSetResourceClaimAssociation)] = o =>
        {
            // Unlike the other entries, this isn't a raw EF entity: the caller builds this
            // small DTO explicitly (see DeleteResouceClaimOnClaimSetCommand) because the
            // command removes potentially several ClaimSetResourceClaimAction rows sharing
            // one ClaimSetId/ResourceClaimId, and the URL already carries those two IDs -
            // what's missing from the audit trail is the claim set/resource claim's natural
            // names and the full set of deleted action IDs, not another representative row.
            var a = (DeletedClaimSetResourceClaimAssociation)o;
            return new { a.ClaimSetName, a.ResourceClaimName, a.ActionIds };
        }
    };

    public object? Project(object entity)
    {
        return _selectors.TryGetValue(entity.GetType(), out var selector)
            ? selector(entity)
            : null;
    }
}
