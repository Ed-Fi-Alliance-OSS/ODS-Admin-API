// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models
{
    /// <summary>
    /// Class representing EdFi client application information persisted in a data store.
    /// A Client has a list of domains that are valid for access
    /// </summary>
    [Table("ApiClients")]
    public class ApiClient
    {
        public ApiClient()
        {
            ClientAccessTokens = new List<ClientAccessToken>();
            ApplicationEducationOrganizations = new Collection<ApplicationEducationOrganization>();
            Domains = new Dictionary<string, string>();
        }

        public ApiClient(bool generateKey = false)
            : this()
        {
            if (!generateKey)
            {
                return;
            }

            Key = GenerateRandomString(12);
            Secret = GenerateRandomString();
        }

        public int ApiClientId { get; set; }

        [Required]
        [StringLength(50)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Secret { get; set; } = string.Empty;

        public bool SecretIsHashed { get; set; }

        [Required]
        [StringLength(50)]
        public required string Name { get; set; } = string.Empty;

        public bool IsApproved { get; set; }

        public bool UseSandbox { get; set; }

        public SandboxType SandboxType { get; set; }

        public string SandboxTypeName => SandboxType.ToString();

        [NotMapped]
        public string Status { get; set; } = string.Empty;

        public string KeyStatus { get; set; } = string.Empty;

        public string ChallengeId { get; set; } = string.Empty;

        public DateTime? ChallengeExpiry { get; set; }

        public string ActivationCode { get; set; } = string.Empty;

        public int? ActivationRetried { get; set; }

        public virtual OwnershipToken? CreatorOwnershipToken { get; set; }

        [Column("CreatorOwnershipTokenId_OwnershipTokenId")]
        public short? CreatorOwnershipTokenId { get; set; }

        [StringLength(306)]
        public string StudentIdentificationSystemDescriptor { get; set; } = string.Empty;

        public virtual Application? Application { get; set; }

        public virtual User? User { get; set; }

        public virtual ICollection<ApplicationEducationOrganization> ApplicationEducationOrganizations { get; set; }

        public virtual List<ClientAccessToken> ClientAccessTokens { get; set; }

        [NotMapped]
        public Dictionary<string, string> Domains { get; set; }

        private static string GenerateRandomString(int length = 24)
        {
            string result;
            var numBytes = (length + 3) / 4 * 3;
            var bytes = new byte[numBytes];

            using (var rng = RandomNumberGenerator.Create())
            {
                do
                {
                    rng.GetBytes(bytes);
                    result = Convert.ToBase64String(bytes);
                }
                while (result.Contains("+") || result.Contains("/"));
            }

            return result.Substring(0, length);
        }

        public string GenerateSecret()
        {
            return Secret = GenerateRandomString();
        }
    }
}
