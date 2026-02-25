using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.AI
{
    public class GenerateRoutineRequestDto
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("responseId")]
        public long ResponseId { get; set; }
    }

    /// <summary>
    /// DTO para la creación de una nueva rutina de entrenamiento.
    /// </summary>
    public class CreateRoutineRequestDto
    {
        [JsonPropertyName("userId")]
        public long UserId { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("trainingDays")]
        public int TrainingDays { get; set; }

        [JsonPropertyName("days")]
        public List<TrainingDayDto> Days { get; set; } = new();
    }

    public class RoutineResponseDto
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("userId")]
        public int? UserId { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }  // corregido el typo

        [JsonPropertyName("trainingDays")]
        public int TrainingDays { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("days")]
        public List<TrainingDayDto> Days { get; set; } = new();
    }

    public class TrainingDayDto
    {
        [JsonPropertyName("dayNumber")]
        public int DayNumber { get; set; }

        [JsonPropertyName("dayName")]
        public string DayName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("exercises")]
        public List<ExerciseDto> Exercises { get; set; } = new();
    }

    public class ExerciseDto
    {
        [JsonPropertyName("exerciseName")]
        public string ExerciseName { get; set; } = string.Empty;

        [JsonPropertyName("sets")]
        public int? Sets { get; set; }

        [JsonPropertyName("reps")]
        public string? Reps { get; set; }

        [JsonPropertyName("restSeconds")]
        public int? RestSeconds { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        [JsonPropertyName("orderIndex")]
        public int OrderIndex { get; set; }
    }
}