// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Contexts;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.DataAccess.Models;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Repositories;
using EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Models;
using EdFi.Ods.AdminApi.Common.Infrastructure.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.AdminConsole.Infrastructure.Services.Instances.Commands
{
    public interface IEditInstanceCommand
    {
        Task<Instance> Execute(int id, IInstanceRequestModel instance);
    }

    public class EditInstanceCommand(ICommandRepository<Instance> instanceCommand, IQueriesRepository<Instance> instanceQuery, IDbContext dbContext) : IEditInstanceCommand
    {
        private readonly ICommandRepository<Instance> _instanceCommand = instanceCommand;
        private readonly IQueriesRepository<Instance> _instanceQuery = instanceQuery;
        private readonly IDbContext _dbContext = dbContext;

        public async Task<Instance> Execute(int id, IInstanceRequestModel instance)
        {
            var existingInstance = await _instanceQuery.Query().SingleOrDefaultAsync(w => w.Id == id) ?? throw new NotFoundException<int>("Instance", id);

            existingInstance.OdsInstanceId = instance.OdsInstanceId;
            existingInstance.TenantId = instance.TenantId;
            existingInstance.TenantName = instance.TenantName ?? string.Empty;
            existingInstance.InstanceName = instance.Name ?? string.Empty;
            existingInstance.InstanceType = instance.InstanceType;
            await UpdateOdsInstanceDerivativesAsync(id, instance.TenantId, instance.OdsInstanceDerivatives);
            await UpdateOdsInstanceContextsAsync(id, instance.TenantId, instance.OdsInstanceContexts);
            await _instanceCommand.UpdateAsync(existingInstance);
            return existingInstance;
        }

        public async Task UpdateOdsInstanceDerivativesAsync(int id, int tenantId, ICollection<OdsInstanceDerivativeModel>? updatedOdsInstanceDerivatives)
        {
            var existingOdsInstanceDerivatives = await _dbContext.OdsInstanceDerivatives
                .Where(d => d.InstanceId == id)
                .ToListAsync();

            if (updatedOdsInstanceDerivatives == null)
            {
                _dbContext.OdsInstanceDerivatives.RemoveRange(existingOdsInstanceDerivatives);
            }
            else
            {
                _dbContext.OdsInstanceDerivatives.RemoveRange(existingOdsInstanceDerivatives);
                foreach (var updatedItem in updatedOdsInstanceDerivatives)
                {
                    _dbContext.OdsInstanceDerivatives.Add(new OdsInstanceDerivative
                    {
                        InstanceId = id,
                        TenantId = tenantId,
                        DerivativeType = Enum.Parse<DerivativeType>(updatedItem.DerivativeType)
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateOdsInstanceContextsAsync(int id, int tenantId, ICollection<OdsInstanceContextModel>? updatedOdsInstanceContexts)
        {
            var existingOdsInstanceContexts = await _dbContext.OdsInstanceContexts
                .Where(c => c.InstanceId == id)
                .ToListAsync();

            if (updatedOdsInstanceContexts == null)
            {
                _dbContext.OdsInstanceContexts.RemoveRange(existingOdsInstanceContexts);
            }
            else
            {
                _dbContext.OdsInstanceContexts.RemoveRange(existingOdsInstanceContexts);
                foreach (var updatedContext in updatedOdsInstanceContexts)
                {
                    _dbContext.OdsInstanceContexts.Add(new OdsInstanceContext
                    {
                        InstanceId = id,
                        TenantId = tenantId,
                        ContextKey = updatedContext.ContextKey,
                        ContextValue = updatedContext.ContextValue
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
        }

    }
}
