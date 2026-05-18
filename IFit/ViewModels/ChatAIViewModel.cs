using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models;
using IFit.Models.Dtos.AI;
using IFit.Services;
using System.Collections.ObjectModel;
using System.Text;

namespace IFit.ViewModels
{
    public partial class ChatAIViewModel : ObservableObject
    {
        #region Services
        private AppUserService _appUserService;
        private AIRoutineService _aiRoutineService;
        private TrainingService _trainingService;
        #endregion

        #region Properties
        private AppUserResponseDto? CurrentUser { get; set; }
        private int? MaxMemoryId { get; set; }
        private List<MessageHistoryDto> _allHistory = new();
        private RoutineResponseDto? _activeRoutine;
        private bool _isFirstMessage = true;
        private bool _startNewConversation = false;

        [ObservableProperty]
        private partial String WelcomeMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial String UserInput { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsBusy { get; set; } = false;

        [ObservableProperty]
        public partial bool IsPanelVisible { get; set; } = false;

        [ObservableProperty]
        public partial ConversationSummary? SelectedConversation { get; set; }

        public ObservableCollection<ChatMessage> Messages { get; } = new();
        public ObservableCollection<ConversationSummary> Conversations { get; } = new();
        #endregion

        #region Constructor
        public ChatAIViewModel(AppUserService appUserService,
            AIRoutineService aiRoutineService,
            TrainingService trainingService)
        {
            _appUserService = appUserService;
            _aiRoutineService = aiRoutineService;
            _trainingService = trainingService;
        }

        public ChatAIViewModel() : this(
            App.GetService<AppUserService>() ?? throw new InvalidOperationException("AppUserService no inicializado"),
            App.GetService<AIRoutineService>() ?? throw new InvalidOperationException("AIRoutineService no inicializado"),
            App.GetService<TrainingService>() ?? throw new InvalidOperationException("TrainingService no inicializado"))
        {
        }
        #endregion

        #region Commands
        [RelayCommand]
        public async Task AppearingAsync()
        {
            var userId = Preferences.Get("UserId", 0L);

            var freshUser = await _appUserService.findUserById(userId);
            if (freshUser == null) return;

            var coachChanged = CurrentUser?.CoachModelTypeName != freshUser.CoachModelTypeName;
            CurrentUser = freshUser;

            var coachName = CurrentUser.CoachModelTypeName;

            if (!string.IsNullOrWhiteSpace(coachName))
            {
                _allHistory = await _aiRoutineService.GetChatHistoryAsync(coachName);

                if (_allHistory.Count > 0 && int.TryParse(_allHistory[0].MemoryId, out var memId))
                    MaxMemoryId = memId;
            }

            await RefreshConversationListAsync();

            if (_activeRoutine == null || coachChanged)
                _activeRoutine = await _trainingService.getLatestActiveRoutineByUserIdAsync(userId);

            MaxMemoryId = null;
            _isFirstMessage = true;
            _startNewConversation = true;
            SelectedConversation = null;
            LoadMessagesForMemory(null);
        }

        [RelayCommand]
        public async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(UserInput) || IsBusy)
                return;

            var userText = UserInput.Trim();
            Messages.Add(new ChatMessage { Text = userText, IsUser = true });
            UserInput = string.Empty;

            IsBusy = true;
            var thinkingMessage = new ChatMessage { Text = "Pensando... 🤔", IsUser = false };
            Messages.Add(thinkingMessage);

            if (MaxMemoryId == null)
            {
                var userId = Preferences.Get("UserId", 0L);
                var coachName = CurrentUser!.CoachModelTypeName ?? string.Empty;
                var conversation = await _aiRoutineService.GetOrCreateConversation(userId, coachName, _startNewConversation);
                _startNewConversation = false;

                if (conversation == null)
                {
                    Messages.Remove(thinkingMessage);
                    IsBusy = false;
                    return;
                }

                MaxMemoryId = conversation.MemoryId;
                await RefreshConversationListAsync();
            }

            // Primer mensaje de la conversación: añadir contexto de rutina activa
            var payload = userText;
            if (_isFirstMessage && _activeRoutine != null)
            {
                payload = userText + " Answer using the following information:\n" + FormatRoutineContext(_activeRoutine);
                _isFirstMessage = false;
            }

            var response = await _aiRoutineService.ChatWithCoach(
                CurrentUser!.CoachModelTypeName, (int)MaxMemoryId!, payload);

            var index = Messages.IndexOf(thinkingMessage);
            if (index >= 0)
                Messages[index] = new ChatMessage
                {
                    Text = response ?? "Lo siento, no pude generar una respuesta. 😞",
                    IsUser = false
                };

            IsBusy = false;
        }

        [RelayCommand]
        public void TogglePanel() => IsPanelVisible = !IsPanelVisible;

        [RelayCommand]
        public void NewConversation()
        {
            MaxMemoryId = null;
            _isFirstMessage = true;
            _startNewConversation = true;
            SelectedConversation = null;
            IsPanelVisible = false;
            LoadMessagesForMemory(null);
        }

        [RelayCommand]
        public void SelectConversation(ConversationSummary summary)
        {
            SelectedConversation = summary;
            MaxMemoryId = summary.Conversation.MemoryId;
            _isFirstMessage = true;
            LoadMessagesForMemory(summary.Conversation.MemoryId.ToString());
            IsPanelVisible = false;
        }
        #endregion

        #region Methods
        private async Task RefreshConversationListAsync()
        {
            var userId = Preferences.Get("UserId", 0L);
            var currentCoach = CurrentUser?.CoachModelTypeName?.ToLower() ?? string.Empty;
            var conversationList = await _aiRoutineService.GetUserConversations(userId);

            Conversations.Clear();
            foreach (var conv in conversationList.Where(c => c.CoachName == currentCoach))
            {
                var lastMsg = _allHistory
                    .Where(m => m.MemoryId == conv.MemoryId.ToString())
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();

                var summary = new ConversationSummary
                {
                    Conversation = conv,
                    PreviewText = lastMsg != null ? StripRagContext(lastMsg.Message) : string.Empty,
                    FormattedDate = conv.LastUsedAt.ToString("dd MMM")
                };

                Conversations.Add(summary);

                if (MaxMemoryId.HasValue && conv.MemoryId == MaxMemoryId.Value)
                    SelectedConversation = summary;
            }
        }

        private void LoadMessagesForMemory(string? memoryId)
        {
            Messages.Clear();
            Messages.Add(new ChatMessage { Text = BuildWelcomeText(), IsUser = false });

            if (string.IsNullOrEmpty(memoryId))
                return;

            var filtered = _allHistory
                .Where(m => m.MemoryId == memoryId)
                .OrderBy(m => m.CreatedAt);

            foreach (var msg in filtered)
            {
                var isUser = IsUserMessageType(msg.MessageType);
                var text = isUser ? StripRagContext(msg.Message) : msg.Message;
                Messages.Add(new ChatMessage { Text = text, IsUser = isUser });
            }
        }

        private string BuildWelcomeText() =>
            $"¡Hola {CurrentUser?.Name}! 💪 Soy {CurrentUser?.CoachModelTypeName}, tu entrenador personal. " +
            "Estoy aquí para ayudarte a sacar el máximo de tu rutina — cualquier duda que tengas, pregúntame sin miedo. " +
            "También puedo recomendarte vídeos y recursos extra sobre tus ejercicios. " +
            "¡Vamos a por ello!";

        private static string FormatRoutineContext(RoutineResponseDto routine)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Rutina activa del usuario ({routine.TrainingDays} días/semana):");

            foreach (var day in routine.Days.OrderBy(d => d.DayNumber))
            {
                sb.AppendLine($"  Día {day.DayNumber} - {day.DayName}:");
                foreach (var ex in day.Exercises.OrderBy(e => e.OrderIndex))
                    sb.AppendLine($"    - {ex.ExerciseName}: {ex.Sets}x{ex.Reps}, descanso {ex.RestSeconds}s");
            }

            return sb.ToString().TrimEnd();
        }

        private static bool IsUserMessageType(string messageType) =>
            messageType.Equals("user", StringComparison.OrdinalIgnoreCase) ||
            messageType.Equals("human", StringComparison.OrdinalIgnoreCase);

        private static string StripRagContext(string message)
        {
            const string separator = " Answer using the following information:";
            var idx = message.IndexOf(separator, StringComparison.OrdinalIgnoreCase);
            return idx >= 0 ? message[..idx].TrimEnd() : message;
        }

        public async Task<int?> GetMaxMemoryId()
        {
            var maxMemoryId = await _aiRoutineService.GetNewMemoryIdFromServer();
            return maxMemoryId ?? null;
        }
        #endregion
    }
}
