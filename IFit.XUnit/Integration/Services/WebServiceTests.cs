using IFit.Models;
using IFit.Models.Dtos.Auth;
using IFit.Services;
using IFit.XUnit.Utils;
using Newtonsoft.Json;
using SQLite;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace IFit.XUnit.Integration.Services
{
    /// <summary>
    /// Pruebas de integración contra API real de iFit
    /// 
    /// IMPORTANTE: 
    /// - Estas pruebas requieren que el servidor esté corriendo
    /// - Usan un usuario de prueba real
    /// - NO usar en CI/CD automático (requieren servidor activo)
    /// 
    /// Para ejecutar:
    /// 1. Iniciar todos los microservicios (API Gateway, User Service, Keycloak, etc.)
    /// 2. dotnet test --filter "Category=Integration"
    /// </summary>
    [Trait("Category", "Integration")]
    public class WebServiceIntegrationTests
    {
        private readonly WebService _webService;
        private const string API_BASE_URL = "http://192.168.1.72:8080/ifit/api/v1";
        private const string REFRESH_ENDPOINT = "/auth/refresh";

        // Datos del usuario de prueba
        private const string TEST_USER_EMAIL = "newuser249 surname249@example.com";
        private const string TEST_USER_PASSWORD = "P@ssword";
        private const int TEST_USER_ID = 1;

        public WebServiceIntegrationTests()
        {
            var httpClient = new HttpClient();
            var fakeStorage = new FakeSecureStorageService();
            var tokenManager = new TokenManager(fakeStorage);

            _webService = new WebService(httpClient, tokenManager, API_BASE_URL, REFRESH_ENDPOINT);
        }

        #region Tests de Autenticación

        [Fact]
        public async Task Login_ConCredencialesValidas_DebeRetornarAuthResponse()
        {
            // Arrange
            var loginRequest = new
            {
                username = TEST_USER_EMAIL,
                password = TEST_USER_PASSWORD
            };

            // Act
            var response = await _webService.PostAsync<object, AuthResponse>(
                "/auth/login",
                loginRequest,
                requiresAuth: false
            );

            // Assert
            Assert.True(response.Success, $"Login falló: {response.ErrorMessage}");
            Assert.NotNull(response.Data);
            Assert.NotEmpty(response.Data.AccessToken);
            Assert.NotEmpty(response.Data.RefreshToken);
            Assert.Equal("Bearer", response.Data.TokenType);
            Assert.True(response.Data.ExpiresIn > 0);
            Assert.NotNull(response.Data.AppUser);
            Assert.Equal(TEST_USER_EMAIL, response.Data.AppUser.Email);
            Assert.Equal(TEST_USER_ID, response.Data.AppUser.Id);
        }

        [Fact]
        public async Task Login_ConCredencialesInvalidas_DebeRetornarError401()
        {
            // Arrange
            var loginRequest = new
            {
                username = TEST_USER_EMAIL,
                password = "password_incorrecto"
            };

            // Act
            var response = await _webService.PostAsync<object, AuthResponse>(
                "/auth/login",
                loginRequest,
                requiresAuth: false
            );

            // Assert
            Assert.False(response.Success);
            Assert.Equal(401, response.StatusCode);
        }

        #endregion

        #region Tests de Usuario Autenticado

        [Fact]
        public async Task GetUserProfile_ConTokenValido_DebeRetornarPerfil()
        {
            // Arrange - Login primero
            await LoginUsuarioDePrueba();

            // Act
            var response = await _webService.GetAsync<AppUser>("/users/" + TEST_USER_ID);

            // Assert
            Assert.True(response.Success, $"Error: {response.ErrorMessage}");
            Assert.NotNull(response.Data);
            Assert.Equal(TEST_USER_ID, response.Data.Id);
            Assert.Equal(TEST_USER_EMAIL, response.Data.Email);
            Assert.Equal("ROLE_USER", response.Data.RoleName);
        }

        [Fact]
        public async Task GetUserProfile_SinToken_DebeRetornarError401()
        {
            // Arrange - Limpiar cualquier token
            await _webService.LogoutAsync();

            // Act
            var response = await _webService.GetAsync<AppUser>("/users/profile");

            // Assert
            Assert.False(response.Success);
            Assert.Equal(401, response.StatusCode);
        }

        [Fact]
        public async Task UpdateUserProfile_ConDatosValidos_DebeActualizar()
        {
            // Arrange - Login primero
            await LoginUsuarioDePrueba();

            var updateData = new
            {
                name = "newuser249 surname249 ACTUALIZADO"
            };

            var response = await _webService.PutAsync<object, AppUser>(
                $"/users/{TEST_USER_ID}",
                updateData
            );

            // Assert
            Assert.True(response.Success, $"Error: {response.ErrorMessage}");
            Assert.NotNull(response.Data);
            Assert.Contains("ACTUALIZADO", response.Data.Name);
        }

        #endregion

        #region Tests de Coaches (si existen en tu API)

        [Fact]
        public async Task GetCoaches_ConTokenValido_DebeRetornarLista()
        {
            // Arrange
            await LoginUsuarioDePrueba();

            // Act
            var response = await _webService.GetAsync<List<Coach>>("/coach-models");

            // Assert
            Assert.True(response.Success, $"Error: {response.ErrorMessage}");
            Assert.NotNull(response.Data);
            // Verificar coaches conocidos: Ronnie, Serena, Eliud, Kael
            Assert.True(response.Data.Count >= 4);
        }

        [Fact]
        public async Task GetCoachById_Ronnie_DebeRetornarRonnie()
        {
            // Arrange
            await LoginUsuarioDePrueba();

            // Act - Suponiendo que Ronnie tiene ID 1
            var response = await _webService.GetAsync<Coach>("/coach-models/2");

            // Assert
            Assert.True(response.Success, $"Error: {response.ErrorMessage}");
            Assert.NotNull(response.Data);
            Assert.Equal("Ronnie", response.Data.Name);
        }

        #endregion

        #region Tests de Experience Levels

        [Fact]
        public async Task GetExperienceLevels_DebeRetornarLista()
        {
            // Arrange
            await LoginUsuarioDePrueba();

            // Act
            var response = await _webService.GetAsync<List<ExperienceLevel>>("/experience-levels");

            // Assert
            Assert.True(response.Success, $"Error: {response.ErrorMessage}");
            Assert.NotNull(response.Data);
            Assert.True(response.Data.Count > 0);
        }

        #endregion

        #region Tests de Cuestionario

        [Fact]
        public async Task GetQuestionnaire_DebeRetornarCuestionarioCompleto()
        {
            // Arrange
            await LoginUsuarioDePrueba();

            // Act
            var response = await _webService.GetAsync<List<Questionnaire>>("/questionnaires");

            // Assert
            Assert.NotNull(response.Data);
        }

        [Fact]
        public async Task CompletarCuestionario_Flujo_DebeTerminarCorrectamente()
        {
            // Arrange - Login primero
            await LoginUsuarioDePrueba();

            // PASO 1: Obtener cuestionario con primera pregunta
            // GET /questionnaires/with-first-question
            var questionnaireResponse = await _webService.GetAsync<QuestionnaireWithFirstQuestionDTO>(
                "/questionnaires/" + 1 + "/with-first-question"
            );

            Assert.True(questionnaireResponse.Success, $"Error: {questionnaireResponse.ErrorMessage}");
            Assert.NotNull(questionnaireResponse.Data);
            Assert.NotNull(questionnaireResponse.Data.FirstQuestion);

            var firstQuestion = questionnaireResponse.Data.FirstQuestion;
            Assert.True(firstQuestion.Options.Count > 0);

            // PASO 2: Iniciar sesión de cuestionario
            // POST /questionnaires/{userId}/start/{questionnaireId}
            var startResponse = await _webService.PostAsync<object, QuestionnaireResponseDTO>(
                $"/questionnaires/{TEST_USER_ID}/start/{questionnaireResponse.Data.Id}",
                null // No body
            );

            Assert.True(startResponse.Success, $"Error al iniciar: {startResponse.ErrorMessage}");
            Assert.NotNull(startResponse.Data);

            var responseId = startResponse.Data.ResponseId;
            var currentQuestion = startResponse.Data.CurrentQuestion;

            Assert.NotNull(currentQuestion);

            // PASO 3: Responder preguntas hasta terminar
            var maxIterations = 20;
            var iteration = 0;

            while (!startResponse.Data.IsCompleted && iteration < maxIterations)
            {
                iteration++;

                if (currentQuestion == null || currentQuestion.Options.Count == 0)
                    break;

                // Elegir primera opción
                var selectedOption = currentQuestion.Options[0];

                // Preparar respuesta según si requiere texto
                var answer = new
                {
                    questionId = currentQuestion.Id,
                    selectedOptionId = selectedOption.Id,
                    additionalText = selectedOption.RequiresTextInput
                        ? "Texto de prueba adicional"
                        : null
                };

                var answerResponse = await _webService.PostAsync<object, QuestionnaireResponseDTO>(
                    $"/questionnaires/responses/{responseId}/answer",
                    answer
                );

                Assert.True(answerResponse.Success,
                    $"Error en iteración {iteration}: {answerResponse.ErrorMessage}");

                startResponse.Data = answerResponse.Data!;
                currentQuestion = answerResponse.Data!.CurrentQuestion;
            }

            // Assert final
            Assert.True(startResponse.Data.IsCompleted, "El cuestionario no se completó");
            Assert.True(iteration < maxIterations, "Loop infinito detectado");
        }

        #endregion

        #region Tests de Refresh Token

        [Fact]
        public async Task RefreshToken_ConTokenValido_DebeRetornarNuevosTokens()
        {
            // Arrange - Login primero
            var authResponse = await LoginUsuarioDePrueba();
            var oldAccessToken = authResponse.AccessToken;

            // Esperar un poco para que el nuevo token sea diferente
            await Task.Delay(2000);

            // Simular expiración del access token (en producción esto pasaría naturalmente)
            // Para esta prueba, vamos a forzar un refresh manualmente
            var refreshRequest = new
            {
                refreshToken = authResponse.RefreshToken
            };

            // Act
            var response = await _webService.PostAsync<object, AuthResponse>(
                "/auth/refresh",
                refreshRequest,
                requiresAuth: false
            );

            // Assert
            Assert.True(response.Success, $"Error: {response.ErrorMessage}");
            Assert.NotNull(response.Data);
            Assert.NotEmpty(response.Data.AccessToken);
            Assert.NotEqual(oldAccessToken, response.Data.AccessToken); // Nuevo token diferente
        }

        #endregion

        #region Tests de Endpoints No Existentes

        [Fact]
        public async Task GetEndpointInexistente_DebeRetornarError404()
        {
            // Arrange
            await LoginUsuarioDePrueba();

            // Act
            var response = await _webService.GetAsync<object>("/endpoint-que-no-existe");

            // Assert
            Assert.False(response.Success);
            Assert.Equal(404, response.StatusCode);
        }

        #endregion

        #region Tests de Flujo Completo

        [Fact]
        public async Task FlujoCompleto_LoginYObtenerPerfil_DebeCompletar()
        {
            // 1. Login
            var loginRequest = new
            {
                username = TEST_USER_EMAIL,
                password = TEST_USER_PASSWORD
            };

            var loginResponse = await _webService.PostAsync<object, AuthResponse>(
                "/auth/login",
                loginRequest,
                requiresAuth: false
            );

            Assert.True(loginResponse.Success);

            // 2. Guardar tokens
            await _webService.SaveAuthenticationAsync(loginResponse.Data!);

            // 3. Verificar autenticación
            var isAuthenticated = await _webService.IsAuthenticatedAsync();
            Assert.True(isAuthenticated);

            // 4. Obtener perfil
            var profileResponse = await _webService.GetAsync<AppUser>("/users/" + TEST_USER_ID);
            Assert.True(profileResponse.Success);
            Assert.Equal(TEST_USER_EMAIL, profileResponse.Data!.Email);

            // 5. Obtener usuario actual desde storage
            var currentUser = await _webService.GetCurrentUserAsync();
            Assert.NotNull(currentUser);
            Assert.Equal(TEST_USER_EMAIL, currentUser.Email);

            // 6. Logout
            await _webService.LogoutAsync();

            // 7. Verificar que ya no está autenticado
            isAuthenticated = await _webService.IsAuthenticatedAsync();
            Assert.False(isAuthenticated);
        }

        #endregion

        #region Métodos Auxiliares

        /// <summary>
        /// Helper para hacer login y guardar tokens
        /// </summary>
        private async Task<AuthResponse> LoginUsuarioDePrueba()
        {
            var loginRequest = new
            {
                username = TEST_USER_EMAIL,
                password = TEST_USER_PASSWORD
            };

            var response = await _webService.PostAsync<object, AuthResponse>(
                "/auth/login",
                loginRequest,
                requiresAuth: false
            );

            if (response.Success && response.Data != null)
            {
                await _webService.SaveAuthenticationAsync(response.Data);
                return response.Data;
            }

            throw new Exception($"Login falló: {response.ErrorMessage}");
        }

        #endregion

        #region Modelos (ajusta según tu API)

        public class Coach
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public class ExperienceLevel
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public class Questionnaire
        {
            public long Id { get; set; }

            public string Name { get; set; }

            public string Description { get; set; }
        }

        public class Question
        {
            public int Id { get; set; }
            public string Text { get; set; } = string.Empty;
            public List<Answer> Answers { get; set; } = new();
        }

        public class Answer
        {
            public int Id { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        #region Modelos de Cuestionario

        public class QuestionnaireResponseDTO
        {
            public long ResponseId { get; set; }
            public bool IsCompleted { get; set; }
            public QuestionDTO? CurrentQuestion { get; set; }
            public string? Message { get; set; }
        }

        public class QuestionnaireWithFirstQuestionDTO
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string? CoachModelTypeName { get; set; }
            public string? CoachModelTypeEmoji { get; set; }
            public string? ExperienceLevelName { get; set; }
            public bool IsEnabled { get; set; }
            public string CreatedAt { get; set; } = string.Empty;
            public string UpdatedAt { get; set; } = string.Empty;
            public QuestionDTO FirstQuestion { get; set; } = null!;
        }

        public class QuestionDTO
        {
            public long Id { get; set; }
            public string Text { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public List<QuestionOptionDTO> Options { get; set; } = new();
        }

        public class QuestionOptionDTO
        {
            public long Id { get; set; }
            public string Text { get; set; } = string.Empty;

            // ← Estos campos estaban faltando
            public bool RequiresTextInput { get; set; }
            public string? TextInputPrompt { get; set; }
            public string? TextInputPlaceholder { get; set; }
        }

        #endregion

        #endregion
    }
}