// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models
{
    public class OdsInstanceComponent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OdsInstanceComponentId { get; set; }

        /// <summary>
        /// The display name of this particular component, for display/use by management tooling
        /// </summary>
        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        /// <summary>
        /// Url at which this component is accessible, for use by management tooling
        /// </summary>
        [Required]
        [StringLength(200)]
        public required string Url { get; set; }

        /// <summary>
        /// Version number of this ODS component
        /// </summary>
        [Required]
        [StringLength(20)]
        public required string Version { get; set; }

        public required virtual OdsInstance OdsInstance { get; set; }
    }
}
