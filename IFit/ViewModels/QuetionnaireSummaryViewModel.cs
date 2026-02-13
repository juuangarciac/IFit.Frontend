using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Helper;
using IFit.Models;
using IFit.Models.Dtos.AI;
using IFit.Models.Dtos.Questionnaire;
using IFit.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private bool _isGenerating = false;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _statusMessage = "Cargando...";

        [ObservableProperty]
        private RoutineDto? _generatedRoutine;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor con inyección de dependencias
        /// </summary>
        /// 

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
            // Obtener resumen del cuestionario para mostrar en la UI
            try
            {
                StatusMessage = "Obteniendo resumen de tu cuestionario...";
                IsLoading = true;

                QuestionnaireResponseSummaryDTO? summary = await _questionnaireService.GetResponseSummary(_responseId);
                if (summary == null)
                {
                    await ErrorHandler.HandleErrorAsync(
                        "No se pudo obtener el resumen de tu cuestionario. " +
                        "Por favor, verifica tu conexión a internet e intenta nuevamente."
                    );
                    return;
                }

                QuestionnaireSummary = summary;
                QuestionnaireName = summary.QuestionnaireName;
                CoachName = Preferences.Get("CoachName", "");
            }
            catch (Exception ex)
            {
                await ErrorHandler.HandleErrorAsync(
                    $"Ocurrió un error al obtener el resumen de tu cuestionario: {ex.Message}"
                );
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
        /// Comando para iniciar la generación de la rutina
        /// </summary>
        [RelayCommand]
        private async Task StartGenerationAsync()
        {
            await Shell.Current.GoToAsync("/AIGenerationRoutineView");
        }

        #endregion

        #region Methods

        
        #endregion
    }
}
