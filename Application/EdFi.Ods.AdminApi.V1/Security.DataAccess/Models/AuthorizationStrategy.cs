// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EdFi.Ods.AdminApi.V1.Security.DataAccess.Models
{
    public class AuthorizationStrategy
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AuthorizationStrategyId { get; set; }

        [StringLength(255)]
        [Required]
        public required string DisplayName { get; set; }

        [StringLength(255)]
        [Required]
        public required string AuthorizationStrategyName { get; set; }

        [Column("Application_ApplicationId")]
        public int ApplicationId { get; set; }

        [Required]
        public required Application Application { get; set; }
    }
}
