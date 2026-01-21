using System.Text.Json.Serialization;

namespace IFit.Models.Dtos.AI
{
    /// <summary>
    /// DTO para enviar/recibir mensajes del chat con AI coaches
    /// </summary>
    public class ChatMessageDto
    {
        [JsonPropertyName("memoryId")]
        public int MemoryId { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        public ChatMessageDto() { }

        public ChatMessageDto(int memoryId, string message)
        {
            MemoryId = memoryId;
            Message = message;
        }
    }
}