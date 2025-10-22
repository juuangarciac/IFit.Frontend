using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IFit.Models
{
    public class AppAnswer
    {
        [JsonPropertyName("id"), PrimaryKey]
        public long Id { get; set; }

        [JsonPropertyName("orderIndex")]
        public int OrderIndex { get; set; }

        [JsonPropertyName("answerText")]
        public string AnswerText { get; set; } = string.Empty;
    }
}
