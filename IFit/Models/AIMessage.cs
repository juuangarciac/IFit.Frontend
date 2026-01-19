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
        public AIMessage(int MemoryId, string Message)
        {
            this.MemoryId = MemoryId;
            this.Message = Message;
        }

        [JsonPropertyName("memoryId"), PrimaryKey]
        public int MemoryId { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
