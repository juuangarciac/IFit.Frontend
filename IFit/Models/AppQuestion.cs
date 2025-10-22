using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IFit.Models
{
    public class AppQuestion
    {
        [JsonPropertyName("id"), PrimaryKey]
        public long Id { get; set; }

        [JsonPropertyName("text")]
        public String QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("isEnabled")]
        public Boolean IsEnabled { get; set; }

        [JsonIgnore]
        public List<AppAnswer> Answers { get; set; } = new List<AppAnswer>();
    }
}
