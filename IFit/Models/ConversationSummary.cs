namespace IFit.Models
{
    public class ConversationSummary
    {
        public CoachConversation Conversation { get; set; } = null!;
        public string PreviewText { get; set; } = string.Empty;
        public string FormattedDate { get; set; } = string.Empty;
    }
}
