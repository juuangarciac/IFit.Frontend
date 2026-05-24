using SQLite;

namespace IFit.Models
{
    public class ExerciseExplanation
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public string ExerciseName { get; set; } = string.Empty;

        public string CoachName { get; set; } = string.Empty;

        public string ExperienceName { get; set; } = string.Empty;

        public string Explanation { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
