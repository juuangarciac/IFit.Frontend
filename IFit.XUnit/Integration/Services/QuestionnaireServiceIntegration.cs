using IFit.Models.Dtos.Questionnaire;
using IFit.Services;
using IFit.XUnit.Utils;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace IFit.XUnit.Integration.Services
{
    /// <summary>
    /// Tests de integración para QuestionnaireService
    /// Verifica el flujo completo del cuestionario incluyendo la navegación del árbol binario,
    /// las llamadas HTTP correctas y la gestión de estados de sesión.
    /// 
    /// Basado en la estructura real de la base de datos de iFit:
    /// - 13 preguntas en total
    /// - Flujo principal: Q1→Q2→Q3→Q4→Q5→Q6→Q7→Q8→Q9→(Q10 o Q11)→Q12→Q13→FIN
    /// - Q9 tiene bifurcación: "Actualmente entreno regular" → Q11, otros → Q10
    /// </summary>
    public class QuestionnaireServiceIntegrationTest
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly FakeSecureStorageService _fakeSecureStorage;
        private readonly TokenManager _tokenManager;
        private readonly WebService _webService;
        private readonly QuestionnaireService _questionnaireService;

        private const string BASE_URL = "http://localhost:8080/ifit/api/v1";
        private const long TEST_USER_ID = 1;

        // IDs de cuestionarios reales
        private const long QUESTIONNAIRE_RONNIE_PRINCIPIANTE = 1;
        private const long QUESTIONNAIRE_SERENA_PRINCIPIANTE = 2;
        private const long QUESTIONNAIRE_ELIUD_INTERMEDIO = 3;
        private const long QUESTIONNAIRE_KAEL_AVANZADO = 4;
        private const long QUESTIONNAIRE_EVALUACION_GENERAL = 5;

        // Estructura del árbol de preguntas basada en data.sql
        private readonly Dictionary<long, QuestionDTO> _questionTree;
        private readonly Dictionary<long, long?> _optionToNextQuestion;

        public QuestionnaireServiceIntegrationTest()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri(BASE_URL)
            };

            _fakeSecureStorage = new FakeSecureStorageService();
            _tokenManager = new TokenManager(_fakeSecureStorage);
            _webService = new WebService(_httpClient, _tokenManager, BASE_URL, "/auth/refresh");
            _questionnaireService = new QuestionnaireService(_webService);

            SetupAuthTokens();

            // Inicializar árbol de preguntas basado en data.sql real
            _questionTree = InitializeQuestionTree();
            _optionToNextQuestion = InitializeOptionMapping();
        }

        #region Setup Methods

        private void SetupAuthTokens()
        {
            _fakeSecureStorage.SetAsync("ifit_access_token", "valid_access_token").Wait();
            _fakeSecureStorage.SetAsync("ifit_refresh_token", "valid_refresh_token").Wait();
            _fakeSecureStorage.SetAsync("ifit_token_expiry", DateTime.UtcNow.AddHours(1).ToString("o")).Wait();
        }

        /// <summary>
        /// Inicializa el árbol de preguntas basado en data.sql real de iFit
        /// 
        /// Estructura del cuestionario:
        /// Q1: ¿Cuál es tu objetivo principal?
        /// Q2: ¿Condiciones médicas?
        /// Q3: ¿Cuántas veces a la semana?
        /// Q4: ¿Dónde prefieres entrenar?
        /// Q5: ¿Cuál es tu edad?
        /// Q6: ¿Peso? (NUMERIC con input)
        /// Q7: ¿Altura? (NUMERIC con input)
        /// Q8: ¿Nivel de actividad diaria?
        /// Q9: ¿Has entrenado antes? (BIFURCACIÓN)
        ///     → Opción 34 "Actualmente entreno regular" → Q11
        ///     → Otras opciones → Q10
        /// Q10: ¿Te sientes cómodo con rutinas?
        /// Q11: ¿Experiencia con fuerza?
        /// Q12: ¿Duración preferida?
        /// Q13: ¿Restricciones dietéticas? (FINAL)
        /// </summary>
        private Dictionary<long, QuestionDTO> InitializeQuestionTree()
        {
            return new Dictionary<long, QuestionDTO>
            {
                [1] = new QuestionDTO
                {
                    Id = 1,
                    Text = "¿Cuál es tu objetivo principal de entrenamiento?",
                    Type = QuestionType.MULTIPLE_CHOICE,
                    Options = new List<OptionDTO>
                    {
                        new OptionDTO { Id = 1, Text = "Perder grasa corporal", RequiresTextInput = false },
                        new OptionDTO { Id = 2, Text = "Ganar masa muscular", RequiresTextInput = false },
                        new OptionDTO { Id = 3, Text = "Mejorar salud general", RequiresTextInput = false },
                        new OptionDTO { Id = 4, Text = "Aumentar resistencia cardiovascular", RequiresTextInput = false },
                        new OptionDTO { Id = 5, Text = "Tonificar y definir", RequiresTextInput = false }
                    }
                },
                [2] = new QuestionDTO
                {
                    Id = 2,
                    Text = "¿Tienes alguna condición médica o lesión que debamos considerar?",
                    Type = QuestionType.MULTIPLE_CHOICE,
                    Options = new List<OptionDTO>
                    {
                        new OptionDTO { Id = 6, Text = "No tengo ninguna limitación", RequiresTextInput = false },
                        new OptionDTO { Id = 7, Text = "Tengo una lesión reciente", RequiresTextInput = true,
                            TextInputPrompt = "¿Podrías describir brevemente tu lesión?",
                            TextInputPlaceholder = "Ej: Esguince de tobillo hace 2 meses" },
                        new OptionDTO { Id = 8, Text = "Tengo una condición médica crónica", RequiresTextInput = true,
                            TextInputPrompt = "¿Qué condición médica tienes?",
                            TextInputPlaceholder = "Ej: Diabetes, hipertensión, asma" },
                        new OptionDTO { Id = 9, Text = "Tengo limitaciones de movilidad", RequiresTextInput = true,
                            TextInputPrompt = "Cuéntanos sobre tus limitaciones de movilidad",
                            TextInputPlaceholder = "Ej: Problemas de rodilla, espalda" }
                    }
                },
                [3] = new QuestionDTO
                {
                    Id = 3,
                    Text = "¿Cuántas veces a la semana puedes entrenar?",
                    Type = QuestionType.MULTIPLE_CHOICE,
                    Options = new List<OptionDTO>
                    {
                        new OptionDTO { Id = 10, Text = "1-2 veces por semana", RequiresTextInput = false },
                        new OptionDTO { Id = 11, Text = "3-4 veces por semana", RequiresTextInput = false },
                        new OptionDTO { Id = 12, Text = "5-6 veces por semana", RequiresTextInput = false },
                        new OptionDTO { Id = 13, Text = "Todos los días", RequiresTextInput = false }
                    }
                },
                [4] = new QuestionDTO
                {
                    Id = 4,
                    Text = "¿Dónde prefieres entrenar?",
                    Type = QuestionType.MULTIPLE_CHOICE,
                    Options = new List<OptionDTO>
                    {
                        new OptionDTO { Id = 14, Text = "Solo en casa (sin equipamiento)", RequiresTextInput = false },
                        new OptionDTO { Id = 15, Text = "Solo en casa (con equipamiento básico)", RequiresTextInput = false },
                        new OptionDTO { Id = 16, Text = "Solo en gimnasio", RequiresTextInput = false },
                        new OptionDTO { Id = 17, Text = "Ambos (casa y gimnasio)", RequiresTextInput = false }
                    }
                },
                [5] = new QuestionDTO
                {
                    Id = 5,
                    Text = "¿Cuál es tu edad?",
                    Type = QuestionType.MULTIPLE_CHOICE,
                    Options = new List<OptionDTO>
                    {
                        new OptionDTO { Id = 18, Text = "Menos de 18 años", RequiresTextInput = false },
                        new OptionDTO { Id = 19, Text = "18-25 años", RequiresTextInput = false },
                        new OptionDTO { Id = 20, Text = "26-35 años", RequiresTextInput = false },
                        new OptionDTO { Id = 21, Text = "36-50 años", RequiresTextInput = false },
                        new OptionDTO { Id = 22, Text = "Más de 50 años", RequiresTextInput = false }
                    }
                },
                [6] = new QuestionDTO
                {
                    Id = 6,
                    Text = "¿Cuál es tu peso actual (kg)?",
                    Type = QuestionType.NUMERIC,
                    Options = new List<OptionDTO>
                    {
                        new OptionDTO { Id = 23, Text = "Ingresar peso", RequiresTextInput = true,
                            TextInputPrompt = "Ingresa tu peso actual en kilogramos",
                            TextInputPlaceholder = "70" }
                    }
                },
                [7] = new QuestionDTO
                {
                    Id = 7,
                    Text = "¿Cuál es tu altura (cm)?",
                    Type = QuestionType.NUMERIC,
                    Options = new List<OptionDTO>
                    {
                        new OptionDTO { Id = 24, Text = "Ingresar altura", RequiresTextInput = true,
                            TextInputPrompt = "Ingresa tu altura en centímetros",
                            TextInputPlaceholder = "175" }
                    }
                },
                [8] = new QuestionDTO
                {
                    Id = 8,
                    Text = "¿Cómo describirías tu nivel de actividad física diaria?",
                    Type = QuestionType.MULTIPLE_CHOICE,
                    Options = new List<OptionDTO>
                    {
                        new OptionDTO { Id = 25, Text = "Sedentario (trabajo de oficina, poca actividad)", RequiresTextInput = false },
                        new OptionDTO { Id = 26, Text = "Ligeramente activo (camino ocasionalmente)", RequiresTextInput = false },
                        new OptionDTO { Id = 27, Text = "Moderadamente activo (camino o me muevo regularmente)", RequiresTextInput = false },
                        new OptionDTO { Id = 28, Text = "Muy activo (trabajo físico o deporte frecuente)", RequiresTextInput = false },
                        new OptionDTO { Id = 29, Text = "Extremadamente activo (atleta o trabajo muy exigente)", RequiresTextInput = false }
                    }
                },
                [9] = new QuestionDTO
                {
                    Id = 9,
                    Text = "¿Has entrenado con regularidad anteriormente?",
                    Type = QuestionType.MULTIPLE_CHOICE,
                    Options = new List<OptionDTO>
                    {
                        // Opciones 30-33 van a Q10, opción 34 va a Q11 (BIFURCACIÓN)
                        new OptionDTO { Id = 30, Text = "Nunca he entrenado", RequiresTextInput = false },
                        new OptionDTO { Id = 31, Text = "Sí, pero hace más de 1 año", RequiresTextInput = false },
                        new OptionDTO { Id = 32, Text = "Sí, hace menos de 1 año", RequiresTextInput = false },
                        new OptionDTO { Id = 33, Text = "Actualmente entreno de forma irregular", RequiresTextInput = false },
                        new OptionDTO { Id = 34, Text = "Actualmente entreno de forma regular", RequiresTextInput = false }
                    }
                },
                [10] = new QuestionDTO
                {
                    Id = 10,
                    Text = "¿Te sientes cómodo/a siguiendo rutinas estructuradas?",
                    Type = QuestionType.MULTIPLE_CHOICE,
                    Options = new List<OptionDTO>
                    {
                        new OptionDTO { Id = 35, Text = "Sí, prefiero planes detallados y estructurados", RequiresTextInput = false },
                        new OptionDTO { Id = 36, Text = "Me gusta tener guía pero con algo de flexibilidad", RequiresTextInput = false },
                        new OptionDTO { Id = 37, Text = "Prefiero entrenar libremente sin planes fijos", RequiresTextInput = false },
                        new OptionDTO { Id = 38, Text = "No estoy seguro/a, nunca he seguido una rutina", RequiresTextInput = false }
                    }
                },
                [11] = new QuestionDTO
                {
                    Id = 11,
                    Text = "¿Tienes experiencia con entrenamiento de fuerza?",
                    Type = QuestionType.MULTIPLE_CHOICE,
                    Options = new List<OptionDTO>
                    {
                        new OptionDTO { Id = 39, Text = "Sí, entreno regularmente con pesas", RequiresTextInput = false },
                        new OptionDTO { Id = 40, Text = "Tengo algo de experiencia pero no soy constante", RequiresTextInput = false },
                        new OptionDTO { Id = 41, Text = "No, soy nuevo/a en entrenamiento de fuerza", RequiresTextInput = false }
                    }
                },
                [12] = new QuestionDTO
                {
                    Id = 12,
                    Text = "¿Prefieres entrenamientos cortos e intensos o largos y moderados?",
                    Type = QuestionType.MULTIPLE_CHOICE,
                    Options = new List<OptionDTO>
                    {
                        new OptionDTO { Id = 42, Text = "Cortos e intensos (20-30 min)", RequiresTextInput = false },
                        new OptionDTO { Id = 43, Text = "Moderados (45-60 min)", RequiresTextInput = false },
                        new OptionDTO { Id = 44, Text = "Largos y pausados (más de 60 min)", RequiresTextInput = false },
                        new OptionDTO { Id = 45, Text = "Variable, depende del día", RequiresTextInput = false }
                    }
                },
                [13] = new QuestionDTO
                {
                    Id = 13,
                    Text = "¿Tienes alguna preferencia alimentaria o restricción dietética?",
                    Type = QuestionType.TEXT_INPUT,
                    Options = new List<OptionDTO>
                    {
                        new OptionDTO { Id = 46, Text = "Sí, tengo restricciones", RequiresTextInput = true,
                            TextInputPrompt = "Por favor describe tus restricciones o preferencias alimentarias",
                            TextInputPlaceholder = "Ej: Vegetariano, intolerancia a lactosa, bajo en carbohidratos" },
                        new OptionDTO { Id = 47, Text = "No, como de todo", RequiresTextInput = false }
                    }
                }
            };
        }

        /// <summary>
        /// Mapeo de cada opción a su siguiente pregunta (null = fin del cuestionario)
        /// Basado en next_question_id de data.sql
        /// </summary>
        private Dictionary<long, long?> InitializeOptionMapping()
        {
            return new Dictionary<long, long?>
            {
                // Q1 (Objetivo) → Q2
                [1] = 2,
                [2] = 2,
                [3] = 2,
                [4] = 2,
                [5] = 2,

                // Q2 (Condiciones) → Q3
                [6] = 3,
                [7] = 3,
                [8] = 3,
                [9] = 3,

                // Q3 (Frecuencia) → Q4
                [10] = 4,
                [11] = 4,
                [12] = 4,
                [13] = 4,

                // Q4 (Lugar) → Q5
                [14] = 5,
                [15] = 5,
                [16] = 5,
                [17] = 5,

                // Q5 (Edad) → Q6
                [18] = 6,
                [19] = 6,
                [20] = 6,
                [21] = 6,
                [22] = 6,

                // Q6 (Peso) → Q7
                [23] = 7,

                // Q7 (Altura) → Q8
                [24] = 8,

                // Q8 (Actividad) → Q9
                [25] = 9,
                [26] = 9,
                [27] = 9,
                [28] = 9,
                [29] = 9,

                // Q9 (Experiencia previa) → BIFURCACIÓN
                [30] = 10, // Nunca he entrenado → Q10
                [31] = 10, // Hace más de 1 año → Q10
                [32] = 10, // Hace menos de 1 año → Q10
                [33] = 10, // Entreno irregular → Q10
                [34] = 11, // Entreno regular → Q11 (rama alternativa)

                // Q10 (Comodidad rutinas) → Q12
                [35] = 12,
                [36] = 12,
                [37] = 12,
                [38] = 12,

                // Q11 (Experiencia fuerza) → Q12
                [39] = 12,
                [40] = 12,
                [41] = 12,

                // Q12 (Duración) → Q13
                [42] = 13,
                [43] = 13,
                [44] = 13,
                [45] = 13,

                // Q13 (Restricciones) → FIN (null)
                [46] = null,
                [47] = null
            };
        }

        private void SetupSequentialHttpResponses(Queue<(HttpStatusCode, object)> responses)
        {
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(() =>
                {
                    if (responses.Count == 0)
                        throw new InvalidOperationException("No more responses configured");

                    var (statusCode, body) = responses.Dequeue();
                    return new HttpResponseMessage
                    {
                        StatusCode = statusCode,
                        Content = new StringContent(
                            JsonSerializer.Serialize(body),
                            Encoding.UTF8,
                            "application/json")
                    };
                });
        }

        private QuestionnaireResponseDTO CreateResponseDTO(
            long responseId,
            QuestionDTO? currentQuestion,
            bool isCompleted,
            int totalAnswered)
        {
            return new QuestionnaireResponseDTO
            {
                ResponseId = responseId,
                CurrentQuestion = currentQuestion,
                IsCompleted = isCompleted,
                TotalQuestionsAnswered = totalAnswered
            };
        }

        private QuestionnaireSummaryDto CreateQuestionnaireSummary(
            long id, string name, string coachName, string emoji, string level)
        {
            return new QuestionnaireSummaryDto
            {
                Id = id,
                Name = name,
                Description = $"Descripción de {name}",
                CoachModelTypeName = coachName,
                CoachModelTypeEmoji = emoji,
                ExperienceLevelName = level,
                IsEnabled = true
            };
        }

        #endregion

        #region Complete Flow Tests - Ruta Principal (Q9 → Q10)

        /// <summary>
        /// Test del flujo completo siguiendo la ruta principal:
        /// Q1→Q2→Q3→Q4→Q5→Q6→Q7→Q8→Q9(opción 30: nunca entrenado)→Q10→Q12→Q13→FIN
        /// Total: 12 preguntas respondidas
        /// </summary>
        [Fact]
        public async Task CompleteQuestionnaireFlow_MainPath_NeverTrained_ShouldCompleteSuccessfully()
        {
            // Arrange
            var responseId = 100L;
            var responses = new Queue<(HttpStatusCode, object)>();

            // Simular respuestas del servidor para cada paso
            // 1. Start → Q1
            responses.Enqueue((HttpStatusCode.Created, CreateResponseDTO(responseId, _questionTree[1], false, 0)));
            // 2. Q1 "Ganar masa muscular" → Q2
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[2], false, 1)));
            // 3. Q2 "No tengo limitación" → Q3
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[3], false, 2)));
            // 4. Q3 "3-4 veces" → Q4
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[4], false, 3)));
            // 5. Q4 "Solo gimnasio" → Q5
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[5], false, 4)));
            // 6. Q5 "26-35 años" → Q6
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[6], false, 5)));
            // 7. Q6 "75 kg" → Q7
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[7], false, 6)));
            // 8. Q7 "180 cm" → Q8
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[8], false, 7)));
            // 9. Q8 "Moderadamente activo" → Q9
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[9], false, 8)));
            // 10. Q9 "Nunca he entrenado" (opción 30) → Q10 (ruta principal)
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[10], false, 9)));
            // 11. Q10 "Prefiero planes estructurados" → Q12
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[12], false, 10)));
            // 12. Q12 "Moderados 45-60min" → Q13
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[13], false, 11)));
            // 13. Q13 "No, como de todo" → FIN
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, null, true, 12)));

            SetupSequentialHttpResponses(responses);

            // Act & Assert - Flujo completo
            // Step 1: Start
            var result = await _questionnaireService.StartQuestionnaire(TEST_USER_ID, QUESTIONNAIRE_RONNIE_PRINCIPIANTE);
            Assert.NotNull(result);
            Assert.Equal(1, result.CurrentQuestion?.Id);
            Assert.Equal("¿Cuál es tu objetivo principal de entrenamiento?", result.CurrentQuestion?.Text);

            // Step 2: Q1 → Q2
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 1, SelectedOptionId = 2 }); // Ganar masa muscular
            Assert.NotNull(result);
            Assert.Equal(2, result.CurrentQuestion?.Id);

            // Step 3: Q2 → Q3
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 2, SelectedOptionId = 6 }); // No tengo limitación
            Assert.NotNull(result);
            Assert.Equal(3, result.CurrentQuestion?.Id);

            // Step 4: Q3 → Q4
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 3, SelectedOptionId = 11 }); // 3-4 veces
            Assert.NotNull(result);
            Assert.Equal(4, result.CurrentQuestion?.Id);

            // Step 5: Q4 → Q5
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 4, SelectedOptionId = 16 }); // Solo gimnasio
            Assert.NotNull(result);
            Assert.Equal(5, result.CurrentQuestion?.Id);

            // Step 6: Q5 → Q6
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 5, SelectedOptionId = 20 }); // 26-35 años
            Assert.NotNull(result);
            Assert.Equal(6, result.CurrentQuestion?.Id);
            Assert.Equal(QuestionType.NUMERIC, result.CurrentQuestion?.Type);

            // Step 7: Q6 → Q7 (con input numérico)
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 6, SelectedOptionId = 23, AdditionalText = "75" });
            Assert.NotNull(result);
            Assert.Equal(7, result.CurrentQuestion?.Id);

            // Step 8: Q7 → Q8 (con input numérico)
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 7, SelectedOptionId = 24, AdditionalText = "180" });
            Assert.NotNull(result);
            Assert.Equal(8, result.CurrentQuestion?.Id);

            // Step 9: Q8 → Q9
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 8, SelectedOptionId = 27 }); // Moderadamente activo
            Assert.NotNull(result);
            Assert.Equal(9, result.CurrentQuestion?.Id);

            // Step 10: Q9 → Q10 (BIFURCACIÓN - ruta principal)
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 9, SelectedOptionId = 30 }); // Nunca he entrenado
            Assert.NotNull(result);
            Assert.Equal(10, result.CurrentQuestion?.Id); // Debe ir a Q10, NO a Q11

            // Step 11: Q10 → Q12
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 10, SelectedOptionId = 35 }); // Prefiero planes estructurados
            Assert.NotNull(result);
            Assert.Equal(12, result.CurrentQuestion?.Id);

            // Step 12: Q12 → Q13
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 12, SelectedOptionId = 43 }); // Moderados 45-60min
            Assert.NotNull(result);
            Assert.Equal(13, result.CurrentQuestion?.Id);
            Assert.Equal(QuestionType.TEXT_INPUT, result.CurrentQuestion?.Type);

            // Step 13: Q13 → FIN
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 13, SelectedOptionId = 47 }); // No, como de todo
            Assert.NotNull(result);
            Assert.True(result.IsCompleted);
            Assert.Null(result.CurrentQuestion);
            Assert.Equal(12, result.TotalQuestionsAnswered);
        }

        #endregion

        #region Complete Flow Tests - Ruta Alternativa (Q9 → Q11)

        /// <summary>
        /// Test del flujo alternativo cuando el usuario ya entrena regularmente:
        /// Q1→Q2→Q3→Q4→Q5→Q6→Q7→Q8→Q9(opción 34: entreno regular)→Q11→Q12→Q13→FIN
        /// Esta ruta salta Q10 y va directamente a Q11
        /// </summary>
        [Fact]
        public async Task CompleteQuestionnaireFlow_AlternativePath_RegularTrainer_ShouldGoToQ11()
        {
            // Arrange
            var responseId = 200L;
            var responses = new Queue<(HttpStatusCode, object)>();

            // Start → Q1
            responses.Enqueue((HttpStatusCode.Created, CreateResponseDTO(responseId, _questionTree[1], false, 0)));
            // Q1 → Q2
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[2], false, 1)));
            // Q2 → Q3
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[3], false, 2)));
            // Q3 → Q4
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[4], false, 3)));
            // Q4 → Q5
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[5], false, 4)));
            // Q5 → Q6
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[6], false, 5)));
            // Q6 → Q7
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[7], false, 6)));
            // Q7 → Q8
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[8], false, 7)));
            // Q8 → Q9
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[9], false, 8)));
            // Q9 "Actualmente entreno regular" (opción 34) → Q11 (RUTA ALTERNATIVA)
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[11], false, 9)));
            // Q11 → Q12
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[12], false, 10)));
            // Q12 → Q13
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[13], false, 11)));
            // Q13 → FIN
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, null, true, 12)));

            SetupSequentialHttpResponses(responses);

            // Act - Ejecutar flujo hasta la bifurcación
            var result = await _questionnaireService.StartQuestionnaire(TEST_USER_ID, QUESTIONNAIRE_RONNIE_PRINCIPIANTE);

            // Responder Q1-Q8 rápidamente
            result = await _questionnaireService.AnswerQuestion(responseId, new AnswerRequestDTO { QuestionId = 1, SelectedOptionId = 2 });
            result = await _questionnaireService.AnswerQuestion(responseId, new AnswerRequestDTO { QuestionId = 2, SelectedOptionId = 6 });
            result = await _questionnaireService.AnswerQuestion(responseId, new AnswerRequestDTO { QuestionId = 3, SelectedOptionId = 11 });
            result = await _questionnaireService.AnswerQuestion(responseId, new AnswerRequestDTO { QuestionId = 4, SelectedOptionId = 16 });
            result = await _questionnaireService.AnswerQuestion(responseId, new AnswerRequestDTO { QuestionId = 5, SelectedOptionId = 20 });
            result = await _questionnaireService.AnswerQuestion(responseId, new AnswerRequestDTO { QuestionId = 6, SelectedOptionId = 23, AdditionalText = "80" });
            result = await _questionnaireService.AnswerQuestion(responseId, new AnswerRequestDTO { QuestionId = 7, SelectedOptionId = 24, AdditionalText = "175" });
            result = await _questionnaireService.AnswerQuestion(responseId, new AnswerRequestDTO { QuestionId = 8, SelectedOptionId = 28 }); // Muy activo

            // Assert - Verificar que estamos en Q9
            Assert.Equal(9, result.CurrentQuestion?.Id);

            // Act - Seleccionar "Actualmente entreno de forma regular" (opción 34)
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 9, SelectedOptionId = 34 });

            // Assert - Debe ir a Q11, NO a Q10 (bifurcación alternativa)
            Assert.NotNull(result);
            Assert.Equal(11, result.CurrentQuestion?.Id);
            Assert.Equal("¿Tienes experiencia con entrenamiento de fuerza?", result.CurrentQuestion?.Text);

            // Continuar hasta el final
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 11, SelectedOptionId = 39 }); // Entreno con pesas
            Assert.Equal(12, result.CurrentQuestion?.Id);

            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 12, SelectedOptionId = 42 }); // Cortos e intensos
            Assert.Equal(13, result.CurrentQuestion?.Id);

            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 13, SelectedOptionId = 46, AdditionalText = "Vegetariano" });

            Assert.True(result.IsCompleted);
            Assert.Equal(12, result.TotalQuestionsAnswered);
        }

        #endregion

        #region Tests with Text Input

        /// <summary>
        /// Test que verifica el manejo de campos con texto adicional requerido
        /// </summary>
        [Fact]
        public async Task AnswerQuestion_WithRequiredTextInput_ShouldIncludeAdditionalText()
        {
            // Arrange
            var responseId = 300L;
            var responses = new Queue<(HttpStatusCode, object)>();

            responses.Enqueue((HttpStatusCode.Created, CreateResponseDTO(responseId, _questionTree[1], false, 0)));
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[2], false, 1)));
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[3], false, 2)));

            SetupSequentialHttpResponses(responses);

            // Act
            var result = await _questionnaireService.StartQuestionnaire(TEST_USER_ID, QUESTIONNAIRE_SERENA_PRINCIPIANTE);
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO { QuestionId = 1, SelectedOptionId = 1 }); // Perder grasa

            // Responder Q2 con opción que requiere texto (lesión)
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO
                {
                    QuestionId = 2,
                    SelectedOptionId = 7, // Tengo una lesión reciente
                    AdditionalText = "Esguince de tobillo hace 3 semanas"
                });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.CurrentQuestion?.Id); // Debe continuar a Q3
        }

        /// <summary>
        /// Test para preguntas numéricas (peso y altura)
        /// </summary>
        [Fact]
        public async Task AnswerQuestion_NumericQuestions_ShouldAcceptNumericInput()
        {
            // Arrange
            var responseId = 400L;
            var responses = new Queue<(HttpStatusCode, object)>();

            // Ir directamente a Q6 (peso)
            responses.Enqueue((HttpStatusCode.Created, CreateResponseDTO(responseId, _questionTree[6], false, 5)));
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[7], false, 6)));
            responses.Enqueue((HttpStatusCode.OK, CreateResponseDTO(responseId, _questionTree[8], false, 7)));

            SetupSequentialHttpResponses(responses);

            // Act - Simular que ya estamos en Q6
            var result = await _questionnaireService.StartQuestionnaire(TEST_USER_ID, QUESTIONNAIRE_EVALUACION_GENERAL);

            // Responder peso
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO
                {
                    QuestionId = 6,
                    SelectedOptionId = 23,
                    AdditionalText = "72.5" // Peso con decimales
                });

            Assert.NotNull(result);
            Assert.Equal(7, result.CurrentQuestion?.Id);
            Assert.Equal(QuestionType.NUMERIC, result.CurrentQuestion?.Type);

            // Responder altura
            result = await _questionnaireService.AnswerQuestion(responseId,
                new AnswerRequestDTO
                {
                    QuestionId = 7,
                    SelectedOptionId = 24,
                    AdditionalText = "178"
                });

            Assert.NotNull(result);
            Assert.Equal(8, result.CurrentQuestion?.Id);
        }

        #endregion

        #region Get All Questionnaires Test

        /// <summary>
        /// Test que verifica la obtención de todos los cuestionarios disponibles
        /// </summary>
        [Fact]
        public async Task GetAllQuestionnaires_ShouldReturnAllFiveQuestionnaires()
        {
            // Arrange
            var expectedList = new List<QuestionnaireSummaryDto>
            {
                CreateQuestionnaireSummary(1, "Ronnie - Fuerza para Principiantes", "Ronnie", "💪", "Principiante"),
                CreateQuestionnaireSummary(2, "Serena - Bienestar para Principiantes", "Serena", "🌸", "Principiante"),
                CreateQuestionnaireSummary(3, "Eliud - Resistencia Intermedia", "Eliud", "👟", "Intermedio"),
                CreateQuestionnaireSummary(4, "Kael - Calistenia Avanzada", "Kael", "🤸‍♂️", "Avanzado"),
                CreateQuestionnaireSummary(5, "Evaluación General de Fitness", null!, null!, null!)
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        JsonSerializer.Serialize(expectedList),
                        Encoding.UTF8,
                        "application/json")
                });

            // Act
            var result = await _questionnaireService.GetAllQuestionnaires();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Count);

            // Verificar cuestionarios específicos
            Assert.Contains(result, q => q.Name == "Ronnie - Fuerza para Principiantes" && q.CoachModelTypeEmoji == "💪");
            Assert.Contains(result, q => q.Name == "Serena - Bienestar para Principiantes" && q.CoachModelTypeEmoji == "🌸");
            Assert.Contains(result, q => q.Name == "Eliud - Resistencia Intermedia" && q.CoachModelTypeEmoji == "👟");
            Assert.Contains(result, q => q.Name == "Kael - Calistenia Avanzada" && q.CoachModelTypeEmoji == "🤸‍♂️");
            Assert.Contains(result, q => q.Name == "Evaluación General de Fitness");
        }

        #endregion

        #region Response Summary Tests

        /// <summary>
        /// Test que verifica la obtención del resumen después de completar el cuestionario
        /// </summary>
        [Fact]
        public async Task GetResponseSummary_AfterCompletion_ShouldReturnAllAnswers()
        {
            // Arrange
            var responseId = 500L;
            var expectedSummary = new QuestionnaireResponseSummaryDTO
            {
                ResponseId = responseId,
                QuestionnaireName = "Ronnie - Fuerza para Principiantes",
                UserName = "Test User",
                StartedAt = DateTime.UtcNow.AddMinutes(-30),
                CompletedAt = DateTime.UtcNow,
                IsCompleted = true,
                Answers = new List<AnswerDTO>
                {
                    new AnswerDTO { QuestionText = "¿Cuál es tu objetivo principal?",
                       SelectedOptionText = "Ganar masa muscular", AnsweredAt = DateTime.UtcNow.AddMinutes(-29) },
                    new AnswerDTO { QuestionText = "¿Condiciones médicas?",
                        SelectedOptionText = "No tengo ninguna limitación", AnsweredAt = DateTime.UtcNow.AddMinutes(-28) },
                    new AnswerDTO { QuestionText = "¿Cuál es tu peso actual?",
                        SelectedOptionText = "Ingresar peso", AdditionalText = "75", AnsweredAt = DateTime.UtcNow.AddMinutes(-25) },
                    // ... más respuestas
                }
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        JsonSerializer.Serialize(expectedSummary),
                        Encoding.UTF8,
                        "application/json")
                });

            // Act
            var result = await _questionnaireService.GetResponseSummary(responseId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(responseId, result.ResponseId);
            Assert.True(result.IsCompleted);
            Assert.NotNull(result.CompletedAt);
            Assert.NotEmpty(result.Answers);
        }

        #endregion

        #region User Responses Tests

        /// <summary>
        /// Test para obtener sesiones activas del usuario
        /// </summary>
        [Fact]
        public async Task GetMyActiveResponses_WithMultipleActive_ShouldReturnAllActive()
        {
            // Arrange
            var activeResponses = new List<QuestionnaireResponse>
            {
                new QuestionnaireResponse
                {
                    Id = 101,
                    UserId = TEST_USER_ID,
                    QuestionnaireId = QUESTIONNAIRE_RONNIE_PRINCIPIANTE,
                    QuestionnaireName = "Ronnie - Fuerza para Principiantes",
                    StartedAt = DateTime.UtcNow.AddDays(-2),
                    IsCompleted = false,
                    TotalQuestionsAnswered = 5
                },
                new QuestionnaireResponse
                {
                    Id = 102,
                    UserId = TEST_USER_ID,
                    QuestionnaireId = QUESTIONNAIRE_EVALUACION_GENERAL,
                    QuestionnaireName = "Evaluación General de Fitness",
                    StartedAt = DateTime.UtcNow.AddDays(-1),
                    IsCompleted = false,
                    TotalQuestionsAnswered = 3
                }
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        JsonSerializer.Serialize(activeResponses),
                        Encoding.UTF8,
                        "application/json")
                });

            // Act
            var result = await _questionnaireService.GetMyActiveResponses();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.False(r.IsCompleted));
        }

        /// <summary>
        /// Test para obtener sesiones completadas del usuario
        /// </summary>
        [Fact]
        public async Task GetMyCompletedResponses_WithCompletedSessions_ShouldReturnOnlyCompleted()
        {
            // Arrange
            var completedResponses = new List<QuestionnaireResponse>
            {
                new QuestionnaireResponse
                {
                    Id = 201,
                    UserId = TEST_USER_ID,
                    QuestionnaireId = QUESTIONNAIRE_SERENA_PRINCIPIANTE,
                    QuestionnaireName = "Serena - Bienestar para Principiantes",
                    StartedAt = DateTime.UtcNow.AddDays(-7),
                    CompletedAt = DateTime.UtcNow.AddDays(-7).AddMinutes(15),
                    IsCompleted = true,
                    TotalQuestionsAnswered = 12
                }
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        JsonSerializer.Serialize(completedResponses),
                        Encoding.UTF8,
                        "application/json")
                });

            // Act
            var result = await _questionnaireService.GetMyCompletedResponses();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.True(result[0].IsCompleted);
            Assert.NotNull(result[0].CompletedAt);
            Assert.Equal(12, result[0].TotalQuestionsAnswered);
        }

        #endregion

        #region Edge Cases

        /// <summary>
        /// Test que verifica el comportamiento cuando se intenta responder un cuestionario ya completado
        /// </summary>
        [Fact]
        public async Task AnswerQuestion_WhenAlreadyCompleted_ShouldReturnNull()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { message = "El cuestionario ya ha sido completado" }),
                        Encoding.UTF8,
                        "application/json")
                });

            // Act
            var result = await _questionnaireService.AnswerQuestion(999,
                new AnswerRequestDTO { QuestionId = 1, SelectedOptionId = 1 });

            // Assert
            Assert.Null(result);
        }

        /// <summary>
        /// Test que verifica el comportamiento con una opción inválida
        /// </summary>
        [Fact]
        public async Task AnswerQuestion_WithInvalidOption_ShouldReturnNull()
        {
            // Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { message = "Opción no encontrada" }),
                        Encoding.UTF8,
                        "application/json")
                });

            // Act
            var result = await _questionnaireService.AnswerQuestion(100,
                new AnswerRequestDTO { QuestionId = 1, SelectedOptionId = 9999 }); // Opción inexistente

            // Assert
            Assert.Null(result);
        }

        #endregion
    }
}