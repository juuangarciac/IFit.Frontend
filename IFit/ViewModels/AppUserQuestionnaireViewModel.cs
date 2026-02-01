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
        private bool _isLoading;
        private bool _hasError;
        private string _errorMessage = string.Empty;

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
        private bool _canGoBack;
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
                    OnPropertyChanged(nameof(CanGoBack));
                }
            }
        }

        /// <summary>
        /// Indica si hay un error activo
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        /// <summary>
        /// Mensaje de error para mostrar al usuario
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
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
        /// Indica si se puede ir a la pregunta anterior
        /// </summary>
        public bool CanGoBack
        {
            get => _canGoBack;
            set => SetProperty(ref _canGoBack, value);
        }

        /// <summary>
        /// Indica si se puede ir a la siguiente pregunta
        /// </summary>
        public bool CanGoNext
        {
            get => _canGoNext;
            set => SetProperty(ref _canGoNext, value);
        }

        /// <summary>
        /// Indica si es la última pregunta
        /// </summary>
        public bool IsLastQuestion
        {
            get => _isLastQuestion;
            set
            {
                if (SetProperty(ref _isLastQuestion, value))
                {
                    OnPropertyChanged(nameof(NextButtonText));
                }
            }
        }

        /// <summary>
        /// Texto del botón "Siguiente" (cambia en la última pregunta)
        /// </summary>
        public string NextButtonText => IsLastQuestion ? "Finalizar" : "Siguiente";

        #endregion

        #region Commands

        public ICommand GoNextCommand { get; }
        public ICommand GoBackCommand { get; }
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
                canExecute: () => CanGoBack && !IsLoading
            );

            // Comando para cerrar el overlay del input de texto
            CloseInputCommand = new Command(() =>
            {
                RequiresTextInput = false;
            });

            // Cargar datos iniciales
            _ = InitializeAsync();
        }

        /// <summary>
        /// Constructor sin parámetros para compatibilidad con XAML
        /// </summary>
        public AppUserQuestionnaireViewModel() : this(
            App.GetService<QuestionnaireService>() ?? throw new InvalidOperationException("QuestionnaireService no registrado"),
            Preferences.Get("UserId", 0L),
            Preferences.Get("CoachId", 0L),
            Preferences.Get("ExperienceLevelId", 0L)) // questionnaireId se obtiene posteriormente, mediante una llamada al servidor
        {
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Inicializa el cuestionario y carga la primera pregunta
        /// </summary>
        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                HasError = false;
                ErrorMessage = string.Empty;

                Debug.WriteLine($"Inicializando cuestionario {_questionnaireId} para usuario {_userId}");

                // 0. Validar parámetros
                if (_userId <= 0)
                {
                    throw new InvalidOperationException("UserId no válido. Asegúrate de estar autenticado.");
                }

                if (_coachId <= 0)
                {
                    throw new InvalidOperationException("CoachId no válido. Asegúrate de haber seleccionado un entrenador.");
                }

                if (_experienceLevelId <= 0)
                {
                    throw new InvalidOperationException("ExperienceLevelId no válido. Asegúrate de haber seleccionado un nivel de experiencia.");
                }

                // 1. Obtener cuestinario
                var questionnaireDto = _questionnaireService.GetQuestionnaireByCoachIdAndExperienceLevelId(_coachId, _experienceLevelId);

                if(questionnaireDto == null)
                {
                    throw new InvalidOperationException("No se ha encontrado un cuestionario para el entrenador y nivel de experiencia seleccionado.");
                }

                _questionnaireId = questionnaireDto.Id;

                if (_questionnaireId <= 0)
                {
                    throw new InvalidOperationException("QuestionnaireId no válido.");
                }

                // 2. Iniciar sesión de cuestionario
                var response = await _questionnaireService.StartQuestionnaire(_userId, _questionnaireId);

                if (response == null)
                {
                    HasError = true;
                    ErrorMessage = "No se pudo iniciar el cuestionario.";

                    await ErrorHandler.HandleErrorAsync(
                        "Error",
                        "No se pudo iniciar el cuestionario. Por favor, intenta nuevamente."
                    );
                    return;
                }

                // 3. Guardar responseId para respuestas futuras
                _responseId = response.ResponseId;
                Debug.WriteLine($"Sesión iniciada con responseId: {_responseId}");

                // 4. Mostrar primera pregunta
                if (response.CurrentQuestion != null)
                {
                    CurrentQuestion = response.CurrentQuestion;
                    CurrentQuestionIndex = 0;
                    UpdateNavigationState();

                    Debug.WriteLine($"Primera pregunta cargada: {CurrentQuestion.Text}");
                }
                else
                {
                    HasError = true;
                    ErrorMessage = "El cuestionario no tiene preguntas.";

                    await ErrorHandler.HandleErrorAsync(
                        "Error",
                        "El cuestionario no tiene preguntas disponibles."
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en InitializeAsync: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                HasError = true;
                ErrorMessage = "Error al cargar el cuestionario.";

                await ErrorHandler.HandleErrorAsync(
                    "Error Inesperado",
                    "No se pudo cargar el cuestionario. Por favor, intenta nuevamente."
                );
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Navigation Methods

        /// <summary>
        /// Avanza a la siguiente pregunta o finaliza el cuestionario
        /// </summary>
        private async Task GoNextAsync()
        {
            try
            {
                if (SelectedOption == null)
                {
                    HasError = true;
                    ErrorMessage = "Por favor, selecciona una opción.";
                    return;
                }

                IsLoading = true;
                HasError = false;
                ErrorMessage = string.Empty;

                Debug.WriteLine($"Respondiendo pregunta {CurrentQuestionNumber}: Opción seleccionada = {SelectedOption.Id}");

                // 1. Crear request de respuesta
                var answerRequest = new AnswerRequestDTO
                {
                    QuestionId = CurrentQuestion!.Id,
                    SelectedOptionId = SelectedOption.Id,
                    AdditionalText = string.IsNullOrWhiteSpace(AdditionalText) ? string.Empty : AdditionalText
                };

                // 2. Enviar respuesta al servidor
                var response = await _questionnaireService.AnswerQuestion(_responseId, answerRequest);

                if (response == null)
                {
                    HasError = true;
                    ErrorMessage = "No se pudo guardar la respuesta.";

                    await ErrorHandler.HandleErrorAsync(
                        "Error",
                        "No se pudo guardar tu respuesta. Por favor, intenta nuevamente."
                    );
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

                    UpdateNavigationState();

                    Debug.WriteLine($"Siguiente pregunta cargada: {CurrentQuestion.Text}");
                }
                else
                {
                    // No debería pasar si IsCompleted es false
                    Debug.WriteLine("Error: No hay siguiente pregunta pero IsCompleted=false");

                    HasError = true;
                    ErrorMessage = "Error al cargar la siguiente pregunta.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en GoNextAsync: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");

                HasError = true;
                ErrorMessage = "Error al procesar la respuesta.";

                await ErrorHandler.HandleErrorAsync(
                    "Error Inesperado",
                    "No se pudo procesar tu respuesta. Por favor, intenta nuevamente."
                );
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Retrocede a la pregunta anterior (si el backend lo soporta)
        /// NOTA: El flujo actual del backend no soporta retroceder.
        /// Esta funcionalidad requeriría cambios en el backend.
        /// </summary>
        private async Task GoBackAsync()
        {
            // TODO: Implementar retroceso si el backend lo soporta
            // Por ahora, el flujo del QuestionnaireService no permite retroceder

            await ErrorHandler.HandleErrorAsync(
                "Función No Disponible",
                "No es posible retroceder a preguntas anteriores en este momento."
            );
        }

        /// <summary>
        /// Actualiza el estado de navegación (botones habilitados/deshabilitados)
        /// </summary>
        private void UpdateNavigationState()
        {
            // Por ahora, no se puede retroceder
            CanGoBack = false; // Cambiar a true si el backend soporta retroceso

            // Se puede avanzar si hay una opción seleccionada
            CanGoNext = HasSelectedOption;

            // Verificar si es la última pregunta
            IsLastQuestion = CurrentQuestionIndex >= TotalQuestions - 1;

            Debug.WriteLine($"Estado navegación - CanGoBack: {CanGoBack}, CanGoNext: {CanGoNext}, IsLastQuestion: {IsLastQuestion}");
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

                // Mostrar mensaje de éxito
                await ErrorHandler.HandleErrorAsync(
                    "¡Felicitaciones!",
                    "Has completado el cuestionario exitosamente."
                );

                // Navegar a la siguiente pantalla (generación de rutina)
                Debug.WriteLine("Navegando a AIGenerationRoutineView");

                // Pasar el responseId a la siguiente vista para generar rutina
                await Shell.Current.GoToAsync($"//AIGenerationRoutineView?responseId={_responseId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en OnQuestionnaireCompleted: {ex.Message}");

                // Aunque haya error en la navegación, el cuestionario ya está completado
                await ErrorHandler.HandleErrorAsync(
                    "Cuestionario Completado",
                    "Tu cuestionario se completó exitosamente, pero hubo un problema al continuar."
                );
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reinicia el cuestionario (útil para testing o si el usuario quiere empezar de nuevo)
        /// </summary>
        public async Task RestartQuestionnaireAsync()
        {
            // Limpiar estado
            SelectedOption = null;
            AdditionalText = string.Empty;
            CurrentQuestion = null;
            CurrentQuestionIndex = 0;
            HasError = false;
            ErrorMessage = string.Empty;

            // Volver a inicializar
            await InitializeAsync();
        }

        #endregion
    }
}