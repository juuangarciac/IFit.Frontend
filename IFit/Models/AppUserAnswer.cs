
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IFit.Models
{
    public class AppUserAnswer
    {
        [JsonPropertyName("questionId"), PrimaryKey]
        public long QuestionId { get; set; }

        [JsonPropertyName("answerId"), PrimaryKey]
        public long AnswerId { get; set; }

        [JsonPropertyName("userId")]
        public long UserId { get; set; }

        [JsonPropertyName("questionnaireId")]
        public long QuestionnaireId { get; set; }
    }
}
