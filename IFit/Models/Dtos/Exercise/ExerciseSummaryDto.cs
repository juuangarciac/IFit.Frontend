using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.Exercise
{
    /// <summary>
    /// DTO de resumen para la lista paginada de ejercicios.
    /// Corresponde al endpoint GET /exercises (Page&lt;ExerciseSummaryDto&gt;).
    /// </summary>
    public class ExerciseSummaryDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("level")]
        public string Level { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("equipment")]
        public string Equipment { get; set; } = string.Empty;

        [JsonPropertyName("mechanic")]
        public string? Mechanic { get; set; }

        [JsonPropertyName("primaryMuscles")]
        public List<string> PrimaryMuscles { get; set; } = new();

        [JsonPropertyName("secondaryMuscles")]
        public List<string> SecondaryMuscles { get; set; } = new();

        [JsonPropertyName("imageUrls")]
        public List<string> ImageUrls { get; set; } = new();
    }
}
