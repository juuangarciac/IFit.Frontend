using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IFit.Models
{
    public class AppQuestionnaire
    {
        [JsonPropertyName("id"), ScaffoldColumn(false), PrimaryKey]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updateAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("coachModelTypeId"), ScaffoldColumn(false)]
        [Required]
        public long CoachModelTypeId { get; set; }

        [JsonPropertyName("experienceLevelId"), ScaffoldColumn(false)]
        public long ExperienceLevelId { get; set; }

        [JsonPropertyName("userIds"), ScaffoldColumn(false)]
        public HashSet<long> UserIds { get; set; } = new HashSet<long>();

        [JsonPropertyName("questionIds"), ScaffoldColumn(false)]
        public HashSet<long> QuestionIds { get; set; } = new HashSet<long>();
    }
}
