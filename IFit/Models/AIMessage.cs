using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IFit.Models
{
    public class AIMessage
    {
        [JsonPropertyName("id"), PrimaryKey]
        public long Id { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
