// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EdFi.Ods.AdminApi.Common.Infrastructure.Models;

public class DbInstance
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public int? OdsInstanceId { get; set; }

    [StringLength(100)]
    public string? OdsInstanceName { get; set; }

    [Required]
    [StringLength(75)]
    public string Status { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string DatabaseTemplate { get; set; } = string.Empty;

    [StringLength(255)]
    public string? DatabaseName { get; set; }

    [Required]
    public DateTime LastRefreshed { get; set; } = DateTime.UtcNow;

    public DateTime? LastModifiedDate { get; set; }
}
