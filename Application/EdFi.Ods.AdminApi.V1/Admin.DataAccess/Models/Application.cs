// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models
{
    public class Application
    {
        public Application()
        {
            ApplicationEducationOrganizations = [];
            ApiClients = [];
            Profiles = [];
            OperationalContextUri = string.Empty;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ApplicationId { get; set; }

        public string? ApplicationName { get; set; }

        [StringLength(255)]
        public string? ClaimSetName { get; set; }

        public virtual Vendor? Vendor { get; set; }

        public virtual OdsInstance? OdsInstance { get; set; }

        [StringLength(255)]
        [Required]
        public required string OperationalContextUri { get; set; }

        public virtual ICollection<ApplicationEducationOrganization> ApplicationEducationOrganizations { get; set; }

        public virtual ICollection<ApiClient> ApiClients { get; set; }

        public virtual ICollection<Profile> Profiles { get; set; }

        public ApplicationEducationOrganization CreateApplicationEducationOrganization(int educationOrganizationId)
            => new()
            {
                EducationOrganizationId = educationOrganizationId,
                Application = this,
                Clients = ApiClients
            };
    }
}
