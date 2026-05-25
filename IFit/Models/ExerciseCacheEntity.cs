using IFit.Models.Dtos.Exercise;
using SQLite;
using System.Text.Json;

namespace IFit.Models;

[Table("ExerciseCache")]
public class ExerciseCacheEntity
{
    [PrimaryKey]
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public string? Mechanic { get; set; }

    // List<string> no soportado nativamente por SQLite-net → serializar a JSON
    public string PrimaryMusclesJson { get; set; } = "[]";
    public string SecondaryMusclesJson { get; set; } = "[]";
    public string ImageUrlsJson { get; set; } = "[]";

    public static ExerciseCacheEntity FromDto(ExerciseSummaryDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Level = dto.Level,
        Category = dto.Category,
        Equipment = dto.Equipment,
        Mechanic = dto.Mechanic,
        PrimaryMusclesJson = JsonSerializer.Serialize(dto.PrimaryMuscles),
        SecondaryMusclesJson = JsonSerializer.Serialize(dto.SecondaryMuscles),
        ImageUrlsJson = JsonSerializer.Serialize(dto.ImageUrls),
    };

    public ExerciseSummaryDto ToDto() => new()
    {
        Id = Id,
        Name = Name,
        Level = Level,
        Category = Category,
        Equipment = Equipment,
        Mechanic = Mechanic,
        PrimaryMuscles = JsonSerializer.Deserialize<List<string>>(PrimaryMusclesJson) ?? new(),
        SecondaryMuscles = JsonSerializer.Deserialize<List<string>>(SecondaryMusclesJson) ?? new(),
        ImageUrls = JsonSerializer.Deserialize<List<string>>(ImageUrlsJson) ?? new(),
    };
}
