// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EdFi.Ods.AdminApi.V1.Admin.DataAccess.Models
{
    public class User
    {
        public User()
        {
            ApiClients = [];
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        public required string Email { get; set; }

        public required string FullName { get; set; }

        public required Vendor Vendor { get; set; }

        public virtual ICollection<ApiClient> ApiClients { get; set; }

        public ApiClient AddSandboxClient(string name, SandboxType sandboxType, string key, string secret)
        {
            var client = new ApiClient(true)
            {
                Name = name,
                IsApproved = true,
                UseSandbox = true,
                SandboxType = sandboxType
            };

            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(secret))
            {
                client.Key = key;
                client.Secret = secret;
            }

            ApiClients.Add(client);
            return client;
        }

        public static User Create(string userEmail, string userName, Vendor vendor)
        {
            return new User
            {
                Email = userEmail,
                FullName = userName,
                Vendor = vendor
            };
        }
    }

    public class RegisterExternalLoginModel
    {
        [Required]
        [Display(Name = "User name")]
        public string UserName { get; set; } = string.Empty;

        public string ExternalLoginData { get; set; } = string.Empty;
    }

    public class LocalPasswordModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class LoginModel
    {
        [Required]
        [Display(Name = "Email Address")]
        public required string EmailAddress { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public required string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterModel
    {
        [Required]
        [Display(Name = "User name")]
        public required string UserName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public required string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public required string ConfirmPassword { get; set; }
    }

    public class ExternalLogin
    {
        public string Provider { get; set; } = string.Empty;

        public string ProviderDisplayName { get; set; } = string.Empty;

        public string ProviderUserId { get; set; } = string.Empty;
    }
}
