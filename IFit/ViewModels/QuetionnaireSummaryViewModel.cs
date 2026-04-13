using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Helper;
using IFit.Models;
using IFit.Models.Dtos.AI;
using IFit.Models.Dtos.Questionnaire;
using IFit.Services;

namespace IFit.ViewModels
{
    public partial class QuetionnaireSummaryViewModel : ObservableObject
    {
        #region Services
        private readonly AIRoutineService _aiRoutineService;
        private readonly QuestionnaireService _questionnaireService;
        private readonly DatabaseService _databaseService;
        #endregion

        #region Fields
        private readonly long _responseId;
        #endregion

        #region Properties

        [ObservableProperty]
        private QuestionnaireResponseSummaryDTO _questionnaireSummary;

        [ObservableProperty]
        private string _questionnaireName = string.Empty;

        [ObservableProperty]
        private string _coachName = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowStartButton))]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _statusMessage = "Cargando...";

        /// <summary>
        /// Controla la visibilidad del título y la info del coach.
        /// Se oculta durante la carga/generación para dar protagonismo al overlay.
        /// </summary>
        public bool ShowStartButton => !IsLoading;

        #endregion

        #region Constructor

        public QuetionnaireSummaryViewModel(AIRoutineService aiService,
            DatabaseService dbService,
            QuestionnaireService questionnaireService,
            long responseId)
        {
            _aiRoutineService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _databaseService = dbService ?? throw new ArgumentNullException(nameof(dbService));
            _questionnaireService = questionnaireService ?? throw new ArgumentNullException(nameof(questionnaireService));
            _responseId = responseId;
        }

        public QuetionnaireSummaryViewModel() : this(
            App.GetService<AIRoutineService>() ?? throw new InvalidOperationException("AIRoutineService no registrado"),
            App.GetService<DatabaseService>() ?? throw new InvalidOperationException("DatabaseService no registrado"),
            App.GetService<QuestionnaireService>() ?? throw new InvalidOperationException("QuestionnaireService no registrado"),
            Preferences.Get("responseId", 0L)
        )
        {
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                StatusMessage = "Obteniendo respuestas...";
                IsLoading = true;

                QuestionnaireResponseSummaryDTO? summary = await _questionnaireService.GetResponseSummary(_responseId);
                if (summary == null)
                {
                    await NotificationService.ShowErrorAsync(
                        "No se pudo obtener el resumen del cuestionario. Verifica tu conexión.");
                    return;
                }

                QuestionnaireSummary = summary;
                QuestionnaireName = summary.QuestionnaireName;
                CoachName = Preferences.Get("CoachName", "");
            }
            catch (Exception ex)
            {
                await NotificationService.ShowErrorAsync(
                    $"Error al cargar el resumen: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Genera la rutina directamente desde esta vista, mostrando el overlay de carga.
        /// Navega a RoutineSummaryView en cuanto el backend responde — sin pasar por AIGenerationRoutineView.
        /// </summary>
        [RelayCommand]
        private async Task StartGenerationAsync()
        {
            try
            {
                AppUser? appUser = await _databaseService.GetCurrentUserAsync();
                if (appUser == null)
                {
                    await NotificationService.ShowErrorAsync(
                        "No hay ningún usuario activo. Por favor, inicia sesión.");
                    return;
                }

                if (_responseId <= 0)
                {
                    await NotificationService.ShowErrorAsync(
                        "No se encontró un cuestionario completado. Por favor, completa el cuestionario.");
                    return;
                }

                StatusMessage = "Tu entrenador está diseñando tu rutina...";
                IsLoading = true;

                string userId = appUser.Id.ToString();
                string? coachType = Preferences.Get("CoachName", "");
                if (string.IsNullOrWhiteSpace(coachType)) coachType = null;

                var routine = await _aiRoutineService.GenerateRoutineAsync(userId, _responseId, coachType);

                if (routine == null)
                {
                    await NotificationService.ShowErrorAsync(
                        "No se pudo generar tu rutina. Verifica tu conexión e intenta nuevamente.");
                    return;
                }

                var navigationParams = new Dictionary<string, object> { { "Routine", routine } };
                await Shell.Current.GoToAsync("//RoutineSummaryView", navigationParams);
            }
            catch (Exception ex)
            {
                await NotificationService.ShowErrorAsync($"Error inesperado: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
            }
        }

        #endregion
    }
}
