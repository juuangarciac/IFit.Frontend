using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IFit.Models
{
    public class CoachModelTypeDto
    {
        [JsonPropertyName("id"), PrimaryKey]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public String Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public String Description { get; set; } = string.Empty;

        [JsonPropertyName("emojiCharacter")]
        public String EmojiCharacter { get; set; } = string.Empty;
    }
}
