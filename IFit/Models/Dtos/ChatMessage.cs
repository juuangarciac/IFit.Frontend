namespace IFit.Models
{
    public class ChatMessage
    {
        public string Text { get; set; } = string.Empty;
        public bool IsUser { get; set; }
        public bool IsAI => !IsUser;
    }
}
