namespace IFit.Models.Dtos.AI
{
    /// <summary>
    /// DTO para obtener el máximo ID de memoria de conversación con AI
    /// </summary>
    public class MaxMemoryIdDto
    {
        public int MaxMemoryId { get; set; }

        public MaxMemoryIdDto() { }

        public MaxMemoryIdDto(int maxMemoryId)
        {
            MaxMemoryId = maxMemoryId;
        }
    }
}