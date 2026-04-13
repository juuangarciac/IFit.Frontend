using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models;
using IFit.Models.Dtos.AI;
using IFit.Services;
using System.Collections.ObjectModel;

namespace IFit.ViewModels
{
    public partial class ChatAIViewModel : ObservableObject
    {
        #region Services
        private AppUserService _appUserService;
        private AIRoutineService _aiRoutineService;
        #endregion

        #region Properties
        private AppUserResponseDto? CurrentUser { get; set; }
        private int? MaxMemoryId { get; set; }

        [ObservableProperty]
        private partial String WelcomeMessage { get; set; } = string.Empty;

        [ObservableProperty]
        public partial String UserInput { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsBusy { get; set; } = false;

        public ObservableCollection<ChatMessage> Messages { get; } = new();
        #endregion

        #region Constructor
        public ChatAIViewModel(AppUserService appUserService,
            AIRoutineService aiRoutineService)
        {
            _appUserService = appUserService;
            _aiRoutineService = aiRoutineService;
        }

        public ChatAIViewModel() : this(
            App.GetService<AppUserService>() ?? throw new InvalidOperationException("AppUserService no inicializado"),
            App.GetService<AIRoutineService>() ?? throw new InvalidOperationException("AIRoutineService no inicializado"))
        {
        }
        #endregion

        #region Commands
        [RelayCommand]
        public async Task AppearingAsync()
        {
            // Fetch user only once; re-show welcome on every visit
            if (CurrentUser == null)
            {
                var userId = Preferences.Get("UserId", 0L);
                CurrentUser = await _appUserService.findUserById(userId);
                if (CurrentUser == null)
                    return;
            }

            var welcomeText = $"¡Hola {CurrentUser.Name}! 💪 Soy {CurrentUser.CoachModelTypeName}, tu entrenador personal. " +
                $"Estoy aquí para ayudarte a sacar el máximo de tu rutina — cualquier duda que tengas, pregúntame sin miedo. " +
                $"También puedo recomendarte vídeos y recursos extra sobre tus ejercicios. " +
                $"¡Vamos a por ello!";

            Messages.Clear();
            Messages.Add(new ChatMessage { Text = welcomeText, IsUser = false });
        }

        [RelayCommand]
        public async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(UserInput) || IsBusy)
                return;

            // Añadir mensaje del usuario
            var userText = UserInput.Trim();
            Messages.Add(new ChatMessage { Text = userText, IsUser = true });
            UserInput = string.Empty;

            // Mostrar "pensando"
            IsBusy = true;
            var thinkingMessage = new ChatMessage { Text = "Pensando... 🤔", IsUser = false };
            Messages.Add(thinkingMessage);

            // Obtener memoryId si no existe
            if (MaxMemoryId == null)
                MaxMemoryId = await GetMaxMemoryId();

            // Llamar al coach
            var response = await _aiRoutineService.ChatWithCoach(
                CurrentUser!.CoachModelTypeName, (int)MaxMemoryId!, userText);

            // Reemplazar "pensando" por la respuesta real
            var index = Messages.IndexOf(thinkingMessage);
            if (index >= 0)
                Messages[index] = new ChatMessage
                {
                    Text = response ?? "Lo siento, no pude generar una respuesta. 😞",
                    IsUser = false
                };

            IsBusy = false;
        }
        #endregion

        #region Methods
        public async Task<int?> GetMaxMemoryId()
        {
            var maxMemoryId = await _aiRoutineService.GetNewMemoryIdFromServer();
            return maxMemoryId ?? null;
        }
        #endregion
    }
}
