// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Validator;

public class InstanceValidator : AbstractValidator<IInstanceRequestModel>
{
    private readonly IQueriesRepository<Instance> _instanceQuery;
    public InstanceValidator(IQueriesRepository<Instance> instanceQuery)
    {
        _instanceQuery = instanceQuery;

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .MustAsync((instance, instanceName, cancellationToken) => BeUniqueForTenant(instance, instanceName, cancellationToken))
            .WithMessage("The name must be unique for the tenant.");

        RuleFor(x => x.InstanceType)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.OdsInstanceContexts)
            .Must(odsInstanceContexts => odsInstanceContexts == null || odsInstanceContexts.GroupBy(c => c.ContextKey).All(g => g.Count() == 1))
            .WithMessage("ContextKey values must be unique within the list.");

        RuleForEach(x => x.OdsInstanceContexts)
            .SetValidator(new OdsInstanceContextValidator());

        RuleForEach(x => x.OdsInstanceDerivatives)
            .SetValidator(new OdsInstanceDerivativeValidator());
    }

    private async Task<bool> BeUniqueForTenant(IInstanceRequestModel instance, string instanceName, CancellationToken cancellationToken)
    {
        var tenantId = instance.TenantId;
        var existingInstance = await _instanceQuery.Query()
            .FirstOrDefaultAsync(i => i.InstanceName == instanceName && i.TenantId == tenantId && i.Id != instance.Id, cancellationToken);
        return existingInstance == null;
    }
}

public class OdsInstanceContextValidator : AbstractValidator<OdsInstanceContextModel>
{
    public OdsInstanceContextValidator()
    {
        RuleFor(x => x.ContextKey)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.ContextValue)
            .NotEmpty()
            .MaximumLength(50);
    }
}

public class OdsInstanceDerivativeValidator : AbstractValidator<OdsInstanceDerivativeModel>
{
    public OdsInstanceDerivativeValidator()
    {
        RuleFor(x => x.DerivativeType)
            .NotEmpty()
            .Must(value => value == nameof(DerivativeType.ReadReplica) || value == nameof(DerivativeType.Snapshot))
            .WithMessage("Allowed values: ReadReplica, Snapshot.");
    }
}
