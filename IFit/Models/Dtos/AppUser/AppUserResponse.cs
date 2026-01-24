using IFit.Models;
using System.Text.Json.Serialization;

public class AppUserResponseDto
{
    [JsonPropertyName("id")]
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
    public string? RoleName { get; set; }

    [JsonPropertyName("coachModelTypeName")]
    public string? CoachModelTypeName { get; set; }

    [JsonPropertyName("experienceLevelName")]
    public string? ExperienceLevelName { get; set; }

    [JsonPropertyName("registrationComplete")]
    public bool RegistrationComplete { get; set; }

    [JsonPropertyName("verified")]
    public bool Verified { get; set; }

    public AppUser toEntity()
    {
        return new AppUser
        {
            Id = this.Id,
            Name = this.Name,
            Email = this.Email,
            CreatedAt = this.CreatedAt,
            UpdatedAt = this.UpdatedAt,
            RoleName = this.RoleName,
            CoachModelTypeName = this.CoachModelTypeName,
            ExperienceLevelName = this.ExperienceLevelName,
            RegistrationComplete = this.RegistrationComplete,
            Verified = this.Verified
        };
    }
}