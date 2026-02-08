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

    public class RoutineResponseDto
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("routine")]
        public RoutineDto? Routine { get; set; }
    }

    public class RoutineDto
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("trainingDays")]
        public int TrainingDays { get; set; }

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
        [JsonPropertyName("exerciseId")]
        public string ExerciseId { get; set; } = string.Empty;

        [JsonPropertyName("exerciseName")]
        public string ExerciseName { get; set; } = string.Empty;

        [JsonPropertyName("sets")]
        public int Sets { get; set; }

        [JsonPropertyName("reps")]
        public string Reps { get; set; } = string.Empty;

        [JsonPropertyName("restSeconds")]
        public int RestSeconds { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;
    }
}
