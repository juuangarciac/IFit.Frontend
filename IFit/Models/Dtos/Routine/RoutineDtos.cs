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

        [JsonPropertyName("coachType")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? CoachType { get; set; }
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

        [JsonPropertyName("currentDay")]
        public int? CurrentDay { get; set; }

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
    // Añadir a RoutineDtos.cs (IFit.Models.Dtos.AI)

    /// <summary>
    /// DTO para la actualización parcial de una rutina existente.
    /// Equivalente a UpdateRoutineRequestDto.java
    /// </summary>
    public class UpdateRoutineRequestDto
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("trainingDays")]
        public int? TrainingDays { get; set; }

        [JsonPropertyName("isActive")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("days")]
        public List<TrainingDayDto>? Days { get; set; }
    }

    /// <summary>
    /// DTO genérico para respuestas paginadas del backend Spring (Page<T>).
    /// Mapea los campos principales que devuelve Spring Data.
    /// </summary>
    public class PagedResponseDto<T>
    {
        [JsonPropertyName("content")]
        public List<T> Content { get; set; } = new();

        [JsonPropertyName("totalElements")]
        public long TotalElements { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("number")]
        public int PageNumber { get; set; }

        [JsonPropertyName("size")]
        public int PageSize { get; set; }

        [JsonPropertyName("first")]
        public bool First { get; set; }

        [JsonPropertyName("last")]
        public bool Last { get; set; }
    }
}