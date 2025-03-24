// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Transactions;
using EdFi.Admin.DataAccess.Contexts;
using EdFi.Admin.DataAccess.Models;
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

    public class EditInstanceCommand(ICommandRepository<Instance> instanceCommand, IQueriesRepository<Instance> instanceQuery, IDbContext dbContext, IUsersContext context) : IEditInstanceCommand
    {
        private readonly ICommandRepository<Instance> _instanceCommand = instanceCommand;
        private readonly IQueriesRepository<Instance> _instanceQuery = instanceQuery;
        private readonly IDbContext _dbContext = dbContext;
        private readonly IUsersContext _context = context;

        public async Task<Instance> Execute(int id, IInstanceRequestModel instance)
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var existingInstance = await _instanceQuery.Query().SingleOrDefaultAsync(w => w.Id == id) ?? throw new NotFoundException<int>("Instance", id);

            if (!instance.Name.Equals(existingInstance.InstanceName, StringComparison.InvariantCultureIgnoreCase) && existingInstance.Status == InstanceStatus.Completed)
            {
                existingInstance.Status = InstanceStatus.Pending_Rename;

                var odsInstance = await _context.OdsInstances
                    .Include(i => i.OdsInstanceContexts)
                    .Include(i => i.OdsInstanceDerivatives)
                    .SingleOrDefaultAsync(w => w.OdsInstanceId == existingInstance.OdsInstanceId);

                var apiclientOdsInstance = await _context.ApiClientOdsInstances
                    .Include(i => i.ApiClient)
                    .SingleOrDefaultAsync(w => w.OdsInstance.OdsInstanceId == existingInstance.OdsInstanceId);

                if (apiclientOdsInstance != null)
                {
                    _context.ApiClientOdsInstances.Remove(apiclientOdsInstance);
                    _context.ApiClients.Remove(apiclientOdsInstance.ApiClient);
                }

                if (odsInstance != null)
                {
                    _context.OdsInstanceContexts.RemoveRange(odsInstance.OdsInstanceContexts);
                    _context.OdsInstanceDerivatives.RemoveRange(odsInstance.OdsInstanceDerivatives);
                    _context.OdsInstances.Remove(odsInstance);
                }
                _context.SaveChanges();
            }

            existingInstance.OdsInstanceId = instance.OdsInstanceId;
            existingInstance.TenantId = instance.TenantId;
            existingInstance.TenantName = instance.TenantName ?? string.Empty;
            existingInstance.InstanceName = instance.Name ?? string.Empty;
            existingInstance.InstanceType = instance.InstanceType;
            await UpdateOdsInstanceDerivativesAsync(id, instance.TenantId, instance.OdsInstanceDerivatives);
            await UpdateOdsInstanceContextsAsync(id, instance.TenantId, instance.OdsInstanceContexts);
            await _instanceCommand.UpdateAsync(existingInstance);

            scope.Complete();
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
                    _dbContext.OdsInstanceDerivatives.Add(new DataAccess.Models.OdsInstanceDerivative
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
                    _dbContext.OdsInstanceContexts.Add(new DataAccess.Models.OdsInstanceContext
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
