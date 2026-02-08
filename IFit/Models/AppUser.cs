using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IFit.Models
{

        /// <summary>
        /// Modelo que representa el usuario de la aplicación
        /// </summary>
        public class AppUser
        {
            [JsonPropertyName("id"), PrimaryKey]
            public long Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("email")]
            public string Email { get; set; } = string.Empty;

            [JsonPropertyName("createdAt")]
            public DateTime CreatedAt { get; set; }

            [JsonPropertyName("updatedAt")]
            public DateTime? UpdatedAt { get; set; }

            [JsonPropertyName("roleName")]
            public string RoleName { get; set; } = string.Empty;

            [JsonPropertyName("coachModelTypeName")]
            public string? CoachModelTypeName { get; set; }

            [JsonPropertyName("experienceLevelName")]
            public string? ExperienceLevelName { get; set; }

            [JsonPropertyName("registrationComplete")]
            public bool RegistrationComplete { get; set; }

            [JsonPropertyName("verified")]
            public bool Verified { get; set; }
        
        public static Boolean isPresent(AppUser? appUser)
        {
            return appUser != null
                && !String.IsNullOrEmpty(appUser.Name) 
                && !String.IsNullOrEmpty(appUser.Email);
        }
    }
}
