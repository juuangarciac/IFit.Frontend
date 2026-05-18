using CommunityToolkit.Mvvm.ComponentModel;
using IFit.Helper;
using IFit.Models.Dtos.Questionnaire;
using IFit.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace IFit.ViewModels
{
    /// <summary>
    /// ViewModel para responder un cuestionario.
    /// Gestiona el flujo de preguntas, respuestas y progreso del usuario.
    /// 
    /// FLUJO:
    /// 1. Usuario selecciona un cuestionario (desde otra vista)
    /// 2. Se inicia una sesión con StartQuestionnaire
    /// 3. Usuario responde pregunta por pregunta
    /// 4. Al completar todas, se marca como completado automáticamente
    /// </summary>
    [QueryProperty(nameof(ResumeResponseId), "ResumeResponseId")]
    public class AppUserQuestionnaireViewModel : ObservableObject
    {
        #region Fields

        private readonly QuestionnaireService _questionnaireService;
        private readonly long _userId;
        private readonly long _coachId;
        private readonly long _experienceLevelId;
        private long _questionnaireId; // No es readonly por que se asigna en InitializeAsync()

        // Estado de la sesión
        private long _responseId;  // ID de la sesión de respuestas
        private long _resumeResponseId;
        private bool _initializationStarted;
        private bool _sessionCompleted;
        private bool _isLoading;
        private string _statusMessage = string.Empty;

        // Datos del cuestionario
        private string _questionnaireName = string.Empty;
        private string _questionnaireDescription = string.Empty;

        // Pregunta actual
        private QuestionDTO? _currentQuestion;
        private int _currentQuestionIndex;
        private int _totalQuestions;

        // Respuesta seleccionada
        private OptionDTO? _selectedOption;
        private string _additionalText = string.Empty;

        // Navegación
        private bool _canGoNext;
        private bool _isLastQuestion;

        #endregion

        #region Properties

        /// <summary>
        /// Indica si se está cargando datos
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (SetProperty(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(CanGoNext));
                }
            }
        }

        /// <summary>
        /// Mensaje con informacion sobre el estado de las solicitudes
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Nombre del cuestionario
        /// </summary>
        public string QuestionnaireName
        {
            get => _questionnaireName;
            set => SetProperty(ref _questionnaireName, value);
        }

        /// <summary>
        /// Descripción del cuestionario
        /// </summary>
        public string QuestionnaireDescription
        {
            get => _questionnaireDescription;
            set => SetProperty(ref _questionnaireDescription, value);
        }

        /// <summary>
        /// Pregunta actual
        /// </summary>
        public QuestionDTO? CurrentQuestion
        {
            get => _currentQuestion;
            set
            {
                if (SetProperty(ref _currentQuestion, value))
                {
                    OnPropertyChanged(nameof(QuestionText));
                    OnPropertyChanged(nameof(Options));
                }

                // Actualizar Options cuando cambia la pregunta
                Options = CurrentQuestion?.Options != null
                    ? new ObservableCollection<OptionDTO>(CurrentQuestion.Options)
                    : new ObservableCollection<OptionDTO>();
            }
        }

        /// <summary>
        /// Texto de la pregunta actual
        /// </summary>
        public string QuestionText => CurrentQuestion?.Text ?? string.Empty;

        /// <summary>
        /// Opciones de respuesta de la pregunta actual
        /// </summary>
        private ObservableCollection<OptionDTO> _options = new();

        public ObservableCollection<OptionDTO> Options
        {
            get => _options;
            set => SetProperty(ref _options, value);
        }

        /// <summary>
        /// Opción seleccionada por el usuario
        /// </summary>
        public OptionDTO? SelectedOption
        {
            get => _selectedOption;
            set
            {
                if (SetProperty(ref _selectedOption, value))
                {
                    // Actualizar RequiresTextInput basado en la opción seleccionada
                    RequiresTextInput = value?.RequiresTextInput ?? false;

                    // IMPORTANTE: Actualizar CanGoNext directamente
                    CanGoNext = HasSelectedOption && !IsLoading;

                    OnPropertyChanged(nameof(HasSelectedOption));

                    // Limpiar texto adicional si cambia la opción
                    AdditionalText = string.Empty;

                    // Reevaluar el comando para habilitar el botón
                    ((Command)GoNextCommand).ChangeCanExecute();
                }
            }
        }

        /// <summary>
        /// Texto adicional opcional para la respuesta
        /// </summary>
        public string AdditionalText
        {
            get => _additionalText;
            set => SetProperty(ref _additionalText, value);
        }

        /// <summary>
        /// Indica si el usuario ha seleccionado una opción
        /// </summary>
        public bool HasSelectedOption => SelectedOption != null;

        /// <summary>
        /// Indica si alguna de las respuesta requiere texto adicional
        /// </summary>
        private bool _requiresTextInput;
        public bool RequiresTextInput
        {
            get => _requiresTextInput;
            set => SetProperty(ref _requiresTextInput, value);
        }

        /// <summary>
        /// Índice de la pregunta actual (basado en 0)
        /// </summary>
        public int CurrentQuestionIndex
        {
            get => _currentQuestionIndex;
            set
            {
                if (SetProperty(ref _currentQuestionIndex, value))
                {
                    OnPropertyChanged(nameof(CurrentQuestionNumber));
                    OnPropertyChanged(nameof(ProgressPercentage));
                    OnPropertyChanged(nameof(ProgressText));
                }
            }
        }

        /// <summary>
        /// Número de pregunta actual (basado en 1 para mostrar al usuario)
        /// </summary>
        public int CurrentQuestionNumber => CurrentQuestionIndex + 1;

        /// <summary>
        /// Total de preguntas del cuestionario
        /// </summary>
        public int TotalQuestions
        {
            get => _totalQuestions;
            set
            {
                if (SetProperty(ref _totalQuestions, value))
                {
                    OnPropertyChanged(nameof(ProgressPercentage));
                    OnPropertyChanged(nameof(ProgressText));
                }
            }
        }

        /// <summary>
        /// Porcentaje de progreso (0.0 a 1.0)
        /// </summary>
        public double ProgressPercentage =>
            TotalQuestions > 0 ? (double)CurrentQuestionNumber / TotalQuestions : 0;

        /// <summary>
        /// Texto de progreso para mostrar al usuario
        /// </summary>
        public string ProgressText => $"Pregunta {CurrentQuestionNumber} de {TotalQuestions}";


        /// <summary>
        /// Indica si se puede ir a la siguiente pregunta
        /// </summary>
        public bool CanGoNext
        {
            get => _canGoNext;
            set => SetProperty(ref _canGoNext, value);
        }


        /// <summary>
        /// Texto del botón "Siguiente" (cambia en la última pregunta)
        /// </summary>
        public string NextButtonText => "Siguiente";

        /// <summary>
        /// ID de sesión de cuestionario previo a reanudar. Shell lo asigna síncronamente
        /// antes de OnAppearing, por lo que el init arrancado desde OnAppearingAsync
        /// ya lee el valor correcto sin necesidad de relanzar nada aquí.
        /// </summary>
        public long ResumeResponseId
        {
            get => _resumeResponseId;
            set => _resumeResponseId = value;
        }

        #endregion

        #region Commands

        public ICommand GoNextCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand SaveTextInputCommand { get; }
        public ICommand CloseInputCommand { get; }

        #endregion

        #region Constructor

        public AppUserQuestionnaireViewModel(
            QuestionnaireService questionnaireService,
            long userId,
            long coachId,
            long experienceLevelId)
        {
            _questionnaireService = questionnaireService ?? throw new ArgumentNullException(nameof(questionnaireService));

            _userId = userId;
            _coachId = coachId;
            _experienceLevelId = experienceLevelId;

            // Inicializar comandos
            GoNextCommand = new Command(
                execute: async () => await GoNextAsync(),
                canExecute: () => HasSelectedOption && !IsLoading
            );

            GoBackCommand = new Command(
                execute: async () => await GoBackAsync(),
                canExecute: () => !IsLoading
            );

            SaveTextInputCommand = new Command(() =>
            {
                RequiresTextInput = false;
            });

            // Comando para cerrar el overlay del input de texto
            CloseInputCommand = new Command(() =>
            {
                RequiresTextInput = false;
                _selectedOption = null;
                OnPropertyChanged(nameof(SelectedOption));
            });
        }

        public AppUserQuestionnaireViewModel() : this(
            App.GetService<QuestionnaireService>() ?? throw new InvalidOperationException("QuestionnaireService no registrado"),
            Preferences.Get("UserId", 0L),
            Preferences.Get("CoachId", 0L),
            Preferences.Get("ExperienceLevelId", 0L))
        {
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Disparado desde OnAppearing de la View. MAUI garantiza que los QueryProperty
        /// (ResumeResponseId) están asignados antes de OnAppearing, así que aquí ya
        /// sabemos si es modo normal o modo reanudación — sin race conditions.
        /// </summary>
        public Task OnAppearingAsync()
        {
            if (_initializationStarted) return Task.CompletedTask;
            _initializationStarted = true;
            IsLoading = true;
            _ = InitializeAsync();
            return Task.CompletedTask;
        }

        private async Task InitializeAsync()
        {
            try
            {
                StatusMessage = "Personalizando cuestionario...";

                Debug.WriteLine($"InitializeAsync — userId={_userId} coachId={_coachId} experienceId={_experienceLevelId} resumeId={_resumeResponseId}");

                if (_userId <= 0)
                    throw new InvalidOperationException("UserId no válido. Asegúrate de estar autenticado.");

                if (_coachId <= 0)
                    throw new InvalidOperationException("CoachId no válido. Asegúrate de haber seleccionado un entrenador.");

                if (_experienceLevelId <= 0)
                    throw new InvalidOperationException("ExperienceLevelId no válido. Asegúrate de haber seleccionado un nivel de experiencia.");

                var questionnaireDto = await _questionnaireService
                    .GetQuestionnaireByCoachIdAndExperienceLevelId(_coachId, _experienceLevelId);

                if (questionnaireDto == null)
                    throw new InvalidOperationException("No se ha encontrado un cuestionario para el entrenador y nivel de experiencia seleccionado.");

                _questionnaireId = questionnaireDto.Id;

                if (_questionnaireId <= 0)
                    throw new InvalidOperationException("QuestionnaireId no válido.");

                TotalQuestions = questionnaireDto.Questions?.Count ?? 0;

                QuestionnaireResponseDTO? response;

                if (_resumeResponseId > 0)
                {
                    StatusMessage = "Cargando última pregunta...";
                    _responseId = _resumeResponseId;

                    response = await _questionnaireService.GoToPreviousQuestion(_responseId);

                    if (response == null)
                    {
                        Debug.WriteLine("Sesión completada sin retroceso disponible, iniciando nueva sesión.");
                        StatusMessage = "Iniciando nuevo cuestionario...";
                        response = await _questionnaireService.StartQuestionnaire(_userId, _questionnaireId);
                        if (response != null) _responseId = response.ResponseId;
                    }
                }
                else
                {
                    response = await _questionnaireService.StartQuestionnaire(_userId, _questionnaireId);
                    if (response != null) _responseId = response.ResponseId;
                }

                if (response == null)
                {
                    await NotificationService.ShowErrorAsync("No se pudo iniciar el cuestionario. Por favor, intenta nuevamente.");
                    return;
                }

                Debug.WriteLine($"Sesión lista — responseId={_responseId} isCompleted={response.IsCompleted} totalAnswered={response.TotalQuestionsAnswered}");

                if (response.CurrentQuestion != null)
                {
                    CurrentQuestion = response.CurrentQuestion;
                    CurrentQuestionIndex = response.TotalQuestionsAnswered;
                    Debug.WriteLine($"Pregunta cargada: index={CurrentQuestionIndex} texto={CurrentQuestion.Text}");
                }
                else
                {
                    await NotificationService.ShowErrorAsync("El cuestionario no tiene preguntas disponibles.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en InitializeAsync: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                await NotificationService.ShowErrorAsync("No se pudo cargar el cuestionario. Por favor, intenta nuevamente.");
            }
            finally
            {
                IsLoading = false;
                StatusMessage = string.Empty;
                UpdateNavigationState();
            }
        }

        #endregion

        #region Navigation Methods

        private void UpdateNavigationState()
        {
            CanGoNext = HasSelectedOption;

            ((Command)GoBackCommand).ChangeCanExecute();
            ((Command)GoNextCommand).ChangeCanExecute();
        }

        /// <summary>
        /// Avanza a la siguiente pregunta o finaliza el cuestionario
        /// </summary>
        private async Task GoNextAsync()
        {
            try
            {
                if (SelectedOption == null)
                {
                    await NotificationService.ShowErrorAsync("Por favor, selecciona una opción.");
                    return;
                }

                IsLoading = true;
                StatusMessage = string.Empty;

                Debug.WriteLine($"Respondiendo pregunta {CurrentQuestionNumber}: Opción seleccionada = {SelectedOption.Id}");

                // 1. Crear request de respuesta (antes de cualquier cambio de estado)
                var answerRequest = new AnswerRequestDTO
                {
                    QuestionId = CurrentQuestion!.Id,
                    SelectedOptionId = SelectedOption.Id,
                    AdditionalText = string.IsNullOrWhiteSpace(AdditionalText) ? string.Empty : AdditionalText
                };

                // 2. Si la sesión estaba completada (usuario volvió desde QuestionnaireSummaryView),
                //    reabrir la sesión antes de responder para que el servidor acepte la respuesta.
                if (_sessionCompleted)
                {
                    var reopenResponse = await _questionnaireService.GoToPreviousQuestion(_responseId);
                    if (reopenResponse == null)
                    {
                        await NotificationService.ShowErrorAsync("No se pudo reabrir el cuestionario. Por favor, intenta nuevamente.");
                        return;
                    }
                    _sessionCompleted = false;
                    Debug.WriteLine($"Sesión reabierta — TotalAnswered={reopenResponse.TotalQuestionsAnswered}");
                }

                // 3. Enviar respuesta al servidor
                var response = await _questionnaireService.AnswerQuestion(_responseId, answerRequest);

                if (response == null)
                {
                    await NotificationService.ShowErrorAsync("No se pudo guardar tu respuesta. Por favor, intenta nuevamente.");
                    return;
                }

                Debug.WriteLine($"Respuesta guardada. IsCompleted: {response.IsCompleted}");

                // 3. Verificar si el cuestionario está completado
                if (response.IsCompleted)
                {
                    Debug.WriteLine("Cuestionario completado exitosamente");

                    await OnQuestionnaireCompleted();
                    return;
                }

                // 4. Cargar siguiente pregunta
                if (response.CurrentQuestion != null)
                {
                    CurrentQuestion = response.CurrentQuestion;
                    CurrentQuestionIndex++;

                    // Limpiar selección anterior
                    SelectedOption = null;
                    AdditionalText = string.Empty;

                    Debug.WriteLine($"Siguiente pregunta cargada: {CurrentQuestion.Text}");
                }
                else
                {
                    // No debería pasar si IsCompleted es false
                    Debug.WriteLine("Error: No hay siguiente pregunta pero IsCompleted=false");
                    await NotificationService.ShowErrorAsync("Error al cargar la siguiente pregunta.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en GoNextAsync: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                await NotificationService.ShowErrorAsync("No se pudo procesar tu respuesta. Por favor, intenta nuevamente.");
            }
            finally
            {
                IsLoading = false;
                UpdateNavigationState();
            }
        }

        /// <summary>
        /// Retrocede a la pregunta anterior deshaciendo la última respuesta registrada
        /// </summary>
        private async Task GoBackAsync()
        {
            try
            {
                IsLoading = true;

                Debug.WriteLine($"Retrocediendo en sesión {_responseId}, pregunta actual: {CurrentQuestionNumber}");

                if(CurrentQuestionIndex <= 0)
                {
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                var response = await _questionnaireService.GoToPreviousQuestion(_responseId);

                if (response == null)
                {
                    await NotificationService.ShowErrorAsync("No se pudo retroceder. Por favor, intenta nuevamente.");
                    return;
                }

                // Cargar la pregunta anterior
                _sessionCompleted = false;
                CurrentQuestion = response.CurrentQuestion;
                CurrentQuestionIndex = response.TotalQuestionsAnswered;

                // Limpiar selección para que el usuario elija de nuevo
                SelectedOption = null;
                AdditionalText = string.Empty;

                Debug.WriteLine($"Retroceso exitoso. Pregunta anterior: {CurrentQuestion?.Text}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en GoBackAsync: {ex.Message}");

                await NotificationService.ShowErrorAsync("No se pudo retroceder. Por favor, intenta nuevamente.");
            }
            finally
            {
                IsLoading = false;
                UpdateNavigationState();
            }
        }

        #endregion

        #region Completion

        /// <summary>
        /// Maneja la finalización del cuestionario
        /// </summary>
        private async Task OnQuestionnaireCompleted()
        {
            try
            {
                Debug.WriteLine($"Cuestionario {_questionnaireId} completado por usuario {_userId}");

                // Navegar a la siguiente pantalla (generación de rutina)
                Debug.WriteLine("Navegando a AIGenerationRoutineView");

                // Pasar el responseId a la siguiente vista para generar rutina
                _sessionCompleted = true;
                Preferences.Set("responseId", _responseId);
                // Push suave hacia el resumen — sin triple slash para evitar el salto brusco
                // del reset de stack. El botón Cancelar de QuestionnaireSummaryView gestiona la salida.
                await Shell.Current.GoToAsync("QuestionnaireSummaryView");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en OnQuestionnaireCompleted: {ex.Message}");

                // Aunque haya error en la navegación, el cuestionario ya está completado
                await NotificationService.ShowErrorAsync("Tu cuestionario se completó exitosamente, pero hubo un problema al continuar.");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reinicia el cuestionario (útil para testing o si el usuario quiere empezar de nuevo)
        /// </summary>
        public async Task RestartQuestionnaireAsync()
        {
            SelectedOption = null;
            AdditionalText = string.Empty;
            CurrentQuestion = null;
            CurrentQuestionIndex = 0;
            _initializationStarted = false;

            await InitializeAsync();
        }

        #endregion
    }
}