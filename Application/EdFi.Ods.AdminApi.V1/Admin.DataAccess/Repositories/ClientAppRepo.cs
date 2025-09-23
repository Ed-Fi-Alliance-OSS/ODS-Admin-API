// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Common;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Contexts;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Extensions;
using EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models;
using log4net;
using Microsoft.EntityFrameworkCore;

namespace EdFi.Ods.AdminApi.V1.Admin.DataAccess.Repositories
{
    public class ClientAppRepo : IClientAppRepo
    {
        private readonly IUsersContextFactory _contextFactory;

        private readonly ILog _logger = LogManager.GetLogger(typeof(ClientAppRepo));
        private readonly Lazy<string> _defaultOperationalContextUri;
        private readonly Lazy<string> _defaultAppName;
        private readonly Lazy<string> _defaultClaimSetName;

        public ClientAppRepo(
            IUsersContextFactory contextFactory,
            IConfigurationRoot config)
        {
            _contextFactory = Preconditions.ThrowIfNull(contextFactory, nameof(contextFactory));
            Preconditions.ThrowIfNull(config, nameof(config));

            _defaultOperationalContextUri = new Lazy<string>(
               () => config.GetSection("DefaultOperationalContextUri")?.Value ?? string.Empty);

            _defaultAppName = new Lazy<string>(
               () => config.GetSection("DefaultApplicationName")?.Value ?? string.Empty);

            _defaultClaimSetName = new Lazy<string>(
               () => config.GetSection("DefaultClaimSetName")?.Value ?? string.Empty);
        }

        private Profile GetOrCreateProfile(string profileName)
        {
            using var context = _contextFactory.CreateContext();
            var profiles = context.Profiles.FirstOrDefault(s => s.ProfileName == profileName);

            if (profiles == null)
            {
                context.Profiles.Add(new Profile { ProfileName = profileName });
                context.SaveChanges();
            }

            return context.Profiles.First(s => s.ProfileName == profileName);
        }

        private OwnershipToken GetOrCreateOwnershipToken(string ownershipToken)
        {
            using var context = _contextFactory.CreateContext();
            var ownershipTokens = context.OwnershipTokens.FirstOrDefault(s => s.Description == ownershipToken);

            if (ownershipTokens == null)
            {
                context.OwnershipTokens.Add(new OwnershipToken { Description = ownershipToken });
                context.SaveChanges();
            }

            return context.OwnershipTokens.First(s => s.Description == ownershipToken);
        }

        public void AddOwnershipTokensToApiClient(string ownershipToken, int apiClientId)
        {
            using var context = _contextFactory.CreateContext();
            var ownershiptoken = GetOrCreateOwnershipToken(ownershipToken);

            var currentOwnershipToken = context.OwnershipTokens
                .Include(u => u.ApiClients)
                .FirstOrDefault(u => u.OwnershipTokenId == ownershiptoken.OwnershipTokenId);

            if (currentOwnershipToken != null)
            {
                var apiClient = context.Clients.FirstOrDefault(a => a.ApiClientId == apiClientId);
                if (apiClient != null && !currentOwnershipToken.ApiClients.Any(a => a.ApiClientId == apiClientId))
                {
                    currentOwnershipToken.ApiClients.Add(apiClient);
                }
            }

            context.SaveChanges();
        }

        public void AddApiClientOwnershipTokens(List<string> ownershipTokens, int apiClientId)
        {
            using var context = _contextFactory.CreateContext();
            var apiClientOwnershipTokenList = new List<ApiClientOwnershipToken>();
            foreach (var ownershipToken in ownershipTokens)
            {
                var ownershiptoken = context.OwnershipTokens.First(x => x.Description == ownershipToken);
                var apiClient = context.Clients.First(u => u.ApiClientId == apiClientId);
                apiClientOwnershipTokenList.Add(new ApiClientOwnershipToken
                {
                    ApiClient = apiClient,
                    OwnershipToken = ownershiptoken
                });
            }
            context.ApiClientOwnershipTokens.AddRange(apiClientOwnershipTokenList);
            context.SaveChanges();
        }

        public void AddProfilesToApplication(List<string> profileNames, int applicationId)
        {
            using var context = _contextFactory.CreateContext();
            foreach (var profileName in profileNames)
            {
                var profile = GetOrCreateProfile(profileName);

                var currentProfile = context.Profiles
                    .Include(u => u.Applications)
                    .First(u => u.ProfileId == profile.ProfileId);

                if (!currentProfile.Applications.Any(a => a.ApplicationId == applicationId))
                {
                    var application = context.Applications.First(a => a.ApplicationId == applicationId);
                    currentProfile.Applications.Add(application);
                }
            }

            context.SaveChanges();
        }

        public User CreateUser(User user)
        {
            using (var context = _contextFactory.CreateContext())
            {
                context.Users.Add(user);
                context.SaveChanges();
            }

            return user;
        }

        public IEnumerable<User> GetUsers()
        {
            using var context = _contextFactory.CreateContext();
            return [.. context.Users.Include(u => u.ApiClients).ThenInclude(ac => ac.Application)];
        }

        public User GetUser(int userId)
        {
            using var context = _contextFactory.CreateContext();
            return
                context.Users.Include(u => u.ApiClients).ThenInclude(ac => ac.Application)
                    .First(u => u.UserId == userId);
        }

        public User GetUser(string userName)
        {
            using var context = _contextFactory.CreateContext();
            return
                context.Users.Include(u => u.ApiClients).ThenInclude(a => a.Application)
                    .Include(u => u.Vendor)
                    .First(x => x.Email == userName);
        }

        public void DeleteUser(User userProfile)
        {
            using var context = _contextFactory.CreateContext();
            var user =
                context.Users.Include(u => u.ApiClients).ThenInclude(ac => ac.Application)
                    .FirstOrDefault(x => x.UserId == userProfile.UserId);

            if (user == null)
            {
                return;
            }

            var arraySoThatUnderlyingCollectionCanBeModified = user.ApiClients.ToArray();

            foreach (var client in arraySoThatUnderlyingCollectionCanBeModified)
            {
                context.Clients.Remove(client);
            }

            context.Users.Remove(user);
            context.SaveChanges();
        }

        public ApiClient? GetClient(string key)
        {
            using var context = _contextFactory.CreateContext();
            return context.Clients
                .Include(c => c.Application)
                    .ThenInclude(c => c!.Vendor)
                    .ThenInclude(c => c!.VendorNamespacePrefixes)
                .Include(c => c.Application)
                    .ThenInclude(c => c!.Profiles)
                .Include(c => c.ApplicationEducationOrganizations)
                .Include(c => c.CreatorOwnershipToken)
                .FirstOrDefault(c => c.Key == key);
        }

        public async Task<ApiClient?> GetClientAsync(string key)
        {
            using var context = _contextFactory.CreateContext();
            return await context.Clients
                .Include(c => c.Application!)
                    .ThenInclude(c => c.Vendor)
                    .ThenInclude(c => c!.VendorNamespacePrefixes)
                .Include(c => c.Application!)
                    .ThenInclude(c => c.Profiles)
                .Include(c => c.ApplicationEducationOrganizations)
                .Include(c => c.CreatorOwnershipTokenId)
                .FirstOrDefaultAsync(c => c.Key == key);
        }

        public ApiClient? GetClient(string key, string secret)
        {
            using var context = _contextFactory.CreateContext();
            return context.Clients.FirstOrDefault(c => c.Key == key && c.Secret == secret);
        }

        public ApiClient? GetClientByKey(string key)
        {
            using var context = _contextFactory.CreateContext();
            return context.Clients.FirstOrDefault(c => c.Key == key);
        }

        public ApiClient UpdateClient(ApiClient client)
        {
            using var context = _contextFactory.CreateContext();
            context.Clients.Update(client);
            context.SaveChanges();
            return client;
        }

        public void DeleteClient(string key)
        {
            using var context = _contextFactory.CreateContext();
            var client = context.Clients.First(x => x.Key == key);

            context.ExecuteSqlCommandAsync(
                @"delete from dbo.ClientAccessTokens where ApiClient_ApiClientId = @p0; delete from dbo.ApiClients where ApiClientId = @p0",
                client.ApiClientId).Wait();
        }

        public Application[] GetVendorApplications(int vendorId)
        {
            using var context = _contextFactory.CreateContext();
            return [.. context.Applications.Where(a => a.Vendor != null && a.Vendor.VendorId == vendorId)];
        }

        public void AddApiClientToUserWithVendorApplication(int userId, ApiClient client)
        {
            using var context = _contextFactory.CreateContext();
            var user = context.Users
                .Include(u => u.Vendor)
                .Include(v => v.Vendor.Applications)
                .SingleOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                return;
            }

            if (user.Vendor != null)
            {
                client.Application = user.Vendor.Applications.FirstOrDefault();
            }

            context.Clients.Add(client);
            context.SaveChanges();
        }

        public ApiClient CreateApiClient(int userId, string name, string key, string secret)
        {
            using var context = _contextFactory.CreateContext();
            var client = CreateApiClient(context, userId, name, SandboxType.Sample, key, secret);

            context.SaveChanges();

            return client;
        }

        public void SetupKeySecret(
            string name,
            SandboxType sandboxType,
            string key,
            string secret,
            int userId,
            int applicationId)
        {
            using var context = _contextFactory.CreateContext();
            var client = CreateApiClient(context, userId, name, sandboxType, key, secret);

            AddApplicationEducationOrganizations(context, applicationId, client);

            context.SaveChanges();
        }

        private ApiClient CreateApiClient(
           IUsersContext context,
           int userId,
           string name,
           SandboxType sandboxType,
           string key,
           string secret)
        {
            var attachedUser = context.Users.Find(userId);

            return attachedUser == null
                ? throw new InvalidOperationException($"User with ID {userId} not found.")
                : attachedUser.AddSandboxClient(name, sandboxType, key, secret);
        }

        public void AddEdOrgIdsToApiClient(int userId, int apiClientId, IList<int> edOrgIds, int applicationId)
        {
            using var context = _contextFactory.CreateContext();
            var application = context.Applications
                .Include(a => a.ApplicationEducationOrganizations)
                .Single(a => a.ApplicationId == applicationId);

            var user = context.Users.FirstOrDefault(u => u.UserId == userId);

            var client = user?.ApiClients.FirstOrDefault(c => c.ApiClientId == apiClientId);

            if (client == null)
            {
                return;
            }

            client.Application = application;

            foreach (var applicationEducationOrganization in application.ApplicationEducationOrganizations.Where(
                s => edOrgIds.Contains(s.EducationOrganizationId)))
            {
                client.ApplicationEducationOrganizations.Add(applicationEducationOrganization);
            }

            context.SaveChanges();
        }

        private void AddApplicationEducationOrganizations(IUsersContext context, int applicationId, ApiClient client)
        {
            var defaultApplication = context.Applications
                .Include(a => a.ApplicationEducationOrganizations)
                .First(a => a.ApplicationId == applicationId);

            client.Application = defaultApplication;

            foreach (var applicationEducationOrganization in defaultApplication.ApplicationEducationOrganizations)
            {
                client.ApplicationEducationOrganizations.Add(applicationEducationOrganization);
            }
        }

        public ApiClient SetupDefaultSandboxClient(
            string name,
            SandboxType sandboxType,
            string key,
            string secret,
            int userId,
            int applicationId)
        {
            using var context = _contextFactory.CreateContext();
            _logger.Debug($"Creating API Client");
            var client = GetClient(key, secret) ?? CreateApiClient(context, userId, name, sandboxType, key, secret);

            _logger.Debug($"Adding Education Organization to client");
            AddApplicationEducationOrganizations(context, applicationId, client);

            context.SaveChanges();

            return client;
        }

        public void Reset()
        {
            try
            {
                using var context = _contextFactory.CreateContext();
                if (context is DbContext dbContext)
                {
                    try
                    {
                        // Admin.Web Creates table webpages_UsersInRoles.  
                        // If exists remove rows, if not swallow exception.  
                        dbContext.DeleteAll<WebPagesUsersInRoles>();
                        context.SaveChanges();
                    }
                    catch (Exception) { }

                    dbContext.DeleteAll<ClientAccessToken>();
                    dbContext.DeleteAll<ApiClient>();
                    dbContext.DeleteAll<User>();
                    dbContext.DeleteAll<ApplicationEducationOrganization>();
                    dbContext.DeleteAll<Application>();
                    dbContext.DeleteAll<Vendor>();
                    dbContext.DeleteAll<OwnershipToken>();
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while attempting to reset Admin database.", ex);
            }
        }

        public void SetDefaultVendorOnUserFromEmailAndName(string userEmail, string userName)
        {
            var namespacePrefix = "uri://" + userEmail.Split('@')[1].ToLower();

            SetDefaultVendorOnUserFromEmailAndName(userEmail, userName, [namespacePrefix]);
        }

        public void SetDefaultVendorOnUserFromEmailAndName(string userEmail, string userName, IEnumerable<string> namespacePrefixes)
        {
            using var context = _contextFactory.CreateContext();
            var vendor = FindOrCreateVendorByDomainName(userName, namespacePrefixes);

            var user = context.Users.FirstOrDefault(u => u.Email.Equals(userEmail));

            if (user == null)
            {
                user = User.Create(userEmail, userName, vendor);
            }
            else
            {
                user.Vendor = vendor;
            }

            context.Vendors.Update(vendor);
            context.Users.Update(user);
            context.SaveChanges();
        }

        public Vendor CreateOrGetVendor(string userEmail, string userName, IEnumerable<string> namespacePrefixes)
        {
            var vendorName = userName.Split(',')[0]
                .Trim();

            using var context = _contextFactory.CreateContext();
            var vendor = context.Vendors.SingleOrDefault(v => v.VendorName == vendorName);

            if (vendor == null)
            {
                vendor = Vendor.Create(vendorName, namespacePrefixes);
                context.SaveChanges();
            }

            return vendor;
        }

        private Vendor FindOrCreateVendorByDomainName(string vendorName, IEnumerable<string> namespacePrefixes)
        {
            using var context = _contextFactory.CreateContext();
            var vendor = context.Vendors.FirstOrDefault(v => v.VendorName == vendorName);

            if (vendor != null)
            {
                return vendor;
            }

            var newVendor = Vendor.Create(vendorName, namespacePrefixes);

            context.Vendors.Update(newVendor);

            //TODO: DEA - Move this behavior to happen during client creation.  No need to do this in two places.  At a minimum, remove the duplicated code.
            CreateDefaultApplicationForVendor(newVendor);

            return newVendor!;
        }

        public Application CreateApplicationForVendor(int vendorId, string applicationName, string claimSetName)
        {
            using var context = _contextFactory.CreateContext();
            var app =
                context.Applications.SingleOrDefault(
                    a => a.ApplicationName == applicationName && a.Vendor != null && a.Vendor.VendorId == vendorId);

            if (app != null)
            {
                return app;
            }

            var vendor = context.Vendors.FirstOrDefault(v => v.VendorId == vendorId);

            app = new Application
            {
                ApplicationName = applicationName,
                Vendor = vendor,
                ClaimSetName = claimSetName,
                OperationalContextUri = _defaultOperationalContextUri.Value
            };

            context.Applications.Update(app);

            context.SaveChanges();

            return app;
        }

        private void CreateDefaultApplicationForVendor(Vendor vendor)
        {
            using var context = _contextFactory.CreateContext();
            var app = context.Applications.SingleOrDefault(
                a => a.ApplicationName == _defaultAppName.Value && a.Vendor != null && a.Vendor.VendorId == vendor.VendorId);

            if (app != null)
            {
                return;
            }

            context.Applications.Update(
                new Application
                {
                    ApplicationName = _defaultAppName.Value,
                    Vendor = vendor,
                    ClaimSetName = _defaultClaimSetName.Value,
                    OperationalContextUri = _defaultOperationalContextUri.Value
                });
        }

        internal class EmailResult
        {
            public string Email { get; set; } = string.Empty;
        }

        internal class ConfirmationTokenResult
        {
            public string ConfirmationToken { get; set; } = string.Empty;
        }
    }
}
