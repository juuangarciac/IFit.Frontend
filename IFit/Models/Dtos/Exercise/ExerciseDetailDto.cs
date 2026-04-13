using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Exercise
{
    /// <summary>
    /// DTO completo para el detalle de un ejercicio individual.
    /// Corresponde al endpoint GET /exercises/{id}.
    /// </summary>
    public class ExerciseDetailDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("force")]
        public string? Force { get; set; }

        [JsonPropertyName("level")]
        public string Level { get; set; } = string.Empty;

        [JsonPropertyName("mechanic")]
        public string? Mechanic { get; set; }

        [JsonPropertyName("equipment")]
        public string Equipment { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("primaryMuscles")]
        public List<string> PrimaryMuscles { get; set; } = new();

        [JsonPropertyName("secondaryMuscles")]
        public List<string> SecondaryMuscles { get; set; } = new();

        [JsonPropertyName("instructions")]
        public List<string> Instructions { get; set; } = new();

        [JsonPropertyName("imageUrls")]
        public List<string> ImageUrls { get; set; } = new();
    }
}
