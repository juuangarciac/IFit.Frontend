using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.ExperienceLevel
{
    /// <summary>
    /// DTO de respuesta para nivel de experiencia
    /// </summary>
    public class ExperienceLevelDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}