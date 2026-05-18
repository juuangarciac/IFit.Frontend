using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.AI
{
    public class MessageHistoryDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("memoryId")]
        public string MemoryId { get; set; } = string.Empty;

        [JsonPropertyName("messageType")]
        public string MessageType { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}
