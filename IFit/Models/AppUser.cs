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
    public class AppUser
    {
        [JsonPropertyName("id"), PrimaryKey]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public String Name { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public String Username { get; set; } = string.Empty;
        [JsonPropertyName("email")]
        public String Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public String Password { get; set; } = string.Empty;

        [JsonPropertyName("verified")]
        public Boolean IsVerified { get; set; } = false;

        [JsonPropertyName("coachModelTypeId"), DefaultValue(0)]
        public long? CoachModelTypeId { get; set; }

        [JsonPropertyName("experienceLevelId"), DefaultValue(0)]
        public long? ExperienceLevelId { get; set; }
        public static Boolean isPresent(AppUser? appUser)
        {
            return appUser != null
                && !String.IsNullOrEmpty(appUser.Name) 
                && !String.IsNullOrEmpty(appUser.Username) 
                && !String.IsNullOrEmpty(appUser.Email) 
                && !String.IsNullOrEmpty(appUser.Password);
        }
    }
}
