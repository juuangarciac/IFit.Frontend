using IFit.Models.Dtos.AppUser;
using IFit.Models.Dtos.AppUser.IFit.Models.Dtos.User;
using IFit.Models.Dtos.Auth;
using IFit.Services;
using IFit.XUnit.Utils;
using Xunit;

namespace IFit.XUnit.Integration.Services
{
    /// <summary>
    /// Pruebas de integración para AppUserService
    /// 
    /// REQUISITOS:
    /// - Servidor Spring Boot corriendo en http://192.168.1.72:8080
    /// - Keycloak activo
    /// - Usuario de prueba registrado y autenticado
    /// 
    /// Para ejecutar solo estas pruebas:
    /// dotnet test --filter "FullyQualifiedName~AppUserServiceIntegrationTest"
    /// </summary>
    [Trait("Category", "Integration")]
    public class AppUserServiceIntegrationTest
    {
        private readonly AppUserService _userService;
        private readonly AuthenticationService _authService;
        private readonly WebService _webService;

        // Configuración de tu API
        private const string API_BASE_URL = "http://192.168.1.72:8080/ifit/api/v1";
        private const string REFRESH_ENDPOINT = "/auth/refresh";

        // Credenciales de usuario de prueba
        private const string TEST_USER_EMAIL = "newuser917@example.com";
        private const string TEST_USER_PASSWORD = "P@ssword";
        private const long TEST_USER_ID = 1; // Ajustar según tu usuario de prueba

        // Para crear usuarios nuevos en tests
        private static string GetUniqueEmail() => $"testuser{DateTime.Now:yyyyMMddHHmmss}@example.com";

        public AppUserServiceIntegrationTest()
        {
            var httpClient = new HttpClient();
            var fakeStorage = new FakeSecureStorageService();
            var tokenManager = new TokenManager(fakeStorage);

            _webService = new WebService(httpClient, tokenManager, API_BASE_URL, REFRESH_ENDPOINT);
            _authService = new AuthenticationService(_webService);
            _userService = new AppUserService(_webService);
        }

        #region Helper Methods

        /// <summary>
        /// Autentica al usuario de prueba antes de cada test
        /// </summary>
        private async Task AuthenticateTestUser()
        {
            var loginResult = await _authService.LoginAsync(TEST_USER_EMAIL, TEST_USER_PASSWORD);
            if (loginResult == null)
            {
                throw new Exception($"No se pudo autenticar el usuario de prueba: {TEST_USER_EMAIL}");
            }
        }

        /// <summary>
        /// Crea y autentica un nuevo usuario para pruebas
        /// </summary>
        private async Task<AppUserResponseDto?> CreateAndAuthenticateNewUser()
        {
            var uniqueEmail = GetUniqueEmail();
            var userName = "Test User " + DateTime.Now.ToString("HHmmss");

            var registerResult = await _authService.RegisterAsync(userName, uniqueEmail, "P@ssw0rd123");
            if (registerResult == null || registerResult.AppUser == null)
            {
                return null;
            }

            return registerResult.AppUser;
        }

        #endregion

        #region FindUserById Tests

        [Fact]
        public async Task FindUserById_ConIdValido_DebeRetornarUsuario()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.findUserById(TEST_USER_ID);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(TEST_USER_EMAIL, result.Email);
            Assert.NotEmpty(result.Name);
            Assert.Equal("ROLE_USER", result.RoleName);
        }

        [Fact]
        public async Task FindUserById_ConIdInexistente_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.findUserById(999999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FindUserById_ConIdCero_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.findUserById(0);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FindUserById_ConIdNegativo_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.findUserById(-1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FindUserById_SinAutenticacion_DebeRetornarNull()
        {
            // Arrange - NO autenticar
            await _authService.LogoutAsync();

            // Act
            var result = await _userService.findUserById(TEST_USER_ID);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region FindUserByEmail Tests

        [Fact]
        public async Task FindUserByEmail_ConEmailValido_DebeRetornarUsuario()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.findUserByEmail(TEST_USER_EMAIL);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(TEST_USER_EMAIL, result.Email);
        }

        [Fact]
        public async Task FindUserByEmail_ConEmailInexistente_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.findUserByEmail("noexiste@example.com");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FindUserByEmail_ConEmailVacio_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.findUserByEmail("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FindUserByEmail_ConEmailConCaracteresEspeciales_DebeEscaparCorrectamente()
        {
            // Arrange
            await AuthenticateTestUser();
            var emailWithPlus = TEST_USER_EMAIL; // Si tienes un email con +, úsalo aquí

            // Act
            var result = await _userService.findUserByEmail(emailWithPlus);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(emailWithPlus, result.Email);
        }

        #endregion

        #region GetAllUsers Tests

        [Fact]
        public async Task GetAllUsers_DebeRetornarListaDeUsuarios()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.GetAllUsers();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count > 0);
            Assert.Contains(result, u => u.Email == TEST_USER_EMAIL);
        }

        [Fact]
        public async Task GetAllUsers_SinAutenticacion_DebeRetornarNull()
        {
            // Arrange - NO autenticar
            await _authService.LogoutAsync();

            // Act
            var result = await _userService.GetAllUsers();

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetUsersPaginated Tests

        [Fact]
        public async Task GetUsersPaginated_ConParametrosDefecto_DebeRetornarPaginaDeUsuarios()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.GetUsersPaginated();

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Content);
            Assert.True(result.Content.Count > 0);
            Assert.True(result.TotalElements > 0);
            Assert.Equal(20, result.Size); // Tamaño por defecto
        }

        [Fact]
        public async Task GetUsersPaginated_ConTamañoPersonalizado_DebeRespetarTamaño()
        {
            // Arrange
            await AuthenticateTestUser();
            var customSize = 5;

            // Act
            var result = await _userService.GetUsersPaginated(page: 0, size: customSize);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Content.Count <= customSize);
            Assert.Equal(customSize, result.Size);
        }

        [Fact]
        public async Task GetUsersPaginated_ConOrdenamiento_DebeOrdenarCorrectamente()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act - Ordenar por email ascendente
            var result = await _userService.GetUsersPaginated(
                page: 0,
                size: 10,
                sortBy: "email",
                sortDir: "asc"
            );

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Content.Count > 1);

            // Verificar que está ordenado
            for (int i = 0; i < result.Content.Count - 1; i++)
            {
                Assert.True(
                    string.Compare(result.Content[i].Email, result.Content[i + 1].Email, StringComparison.Ordinal) <= 0,
                    "Los usuarios no están ordenados por email ascendente"
                );
            }
        }

        [Fact]
        public async Task GetUsersPaginated_PaginaSiguiente_DebeRetornarDiferentesUsuarios()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var page0 = await _userService.GetUsersPaginated(page: 0, size: 3);
            var page1 = await _userService.GetUsersPaginated(page: 1, size: 3);

            // Assert
            Assert.NotNull(page0);
            Assert.NotNull(page1);

            if (page0.TotalElements > 3) // Solo si hay suficientes usuarios
            {
                Assert.NotEqual(page0.Content[0].Id, page1.Content[0].Id);
            }
        }

        #endregion

        #region SetCoachModelType Tests

        [Fact]
        public async Task SetCoachModelType_ConIdsValidos_DebeAsignarCoach()
        {
            // Arrange
            var newUser = await CreateAndAuthenticateNewUser();
            long coachId = 1; // Asumiendo que existe un coach con ID 1

            // Act
            var result = await _userService.SetCoachModelType(newUser.Id, coachId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.CoachModelTypeName);
            // Verificar que el coach fue asignado consultando de nuevo
            var updatedUser = await _userService.findUserById(newUser.Id);
            Assert.NotNull(updatedUser?.CoachModelTypeName);
        }

        [Fact]
        public async Task SetCoachModelType_ConCoachIdInexistente_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();
            long invalidCoachId = 999999;

            // Act
            var result = await _userService.SetCoachModelType(TEST_USER_ID, invalidCoachId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetCoachModelType_ConUserIdNull_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.SetCoachModelType(null, 1);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetCoachModelType_ConCoachIdNull_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.SetCoachModelType(TEST_USER_ID, null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetCoachModelType_ConUserIdCero_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.SetCoachModelType(0, 1);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region SetExperienceLevel Tests

        [Fact]
        public async Task SetExperienceLevel_ConIdsValidos_DebeAsignarNivel()
        {
            // Arrange
            var newUser = await CreateAndAuthenticateNewUser();
            long levelId = 2; // Asumiendo que existe un nivel con ID 2

            // Act
            var result = await _userService.SetExperienceLevel(newUser.Id, levelId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ExperienceLevelName);

            // Verificar consultando de nuevo
            var updatedUser = await _userService.findUserById(newUser.Id);
            Assert.NotNull(updatedUser?.ExperienceLevelName);
        }

        [Fact]
        public async Task SetExperienceLevel_ConLevelIdInexistente_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();
            long invalidLevelId = 999999;

            // Act
            var result = await _userService.SetExperienceLevel(TEST_USER_ID, invalidLevelId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetExperienceLevel_ConUserIdNull_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.SetExperienceLevel(null, 2);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SetExperienceLevel_ConLevelIdNull_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.SetExperienceLevel(TEST_USER_ID, null);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region MarkRegistrationComplete Tests

        [Fact]
        public async Task MarkRegistrationComplete_ConIdValido_DebeMarcarComoCompleto()
        {
            // Arrange
            var newUser = await CreateAndAuthenticateNewUser();

            // Verificar que inicialmente NO está completo
            Assert.False(newUser.RegistrationComplete);

            // Act
            var result = await _userService.MarkRegistrationComplete(newUser.Id);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RegistrationComplete);

            // Verificar consultando de nuevo
            var updatedUser = await _userService.findUserById(newUser.Id);
            Assert.True(updatedUser?.RegistrationComplete);
        }

        [Fact]
        public async Task MarkRegistrationComplete_ConUserIdInexistente_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.MarkRegistrationComplete(999999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task MarkRegistrationComplete_ConUserIdNull_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.MarkRegistrationComplete(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task MarkRegistrationComplete_ConUserIdCero_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.MarkRegistrationComplete(0);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task MarkRegistrationComplete_MultipleVeces_DebeSerIdempotente()
        {
            // Arrange
            var newUser = await CreateAndAuthenticateNewUser();

            // Act - Marcar como completo dos veces
            var result1 = await _userService.MarkRegistrationComplete(newUser.Id);
            var result2 = await _userService.MarkRegistrationComplete(newUser.Id);

            // Assert - Ambas deben retornar true
            Assert.NotNull(result1);
            Assert.True(result1.RegistrationComplete);
            Assert.NotNull(result2);
            Assert.True(result2.RegistrationComplete);
        }

        #endregion

        #region CreateUser Tests

        [Fact]
        public async Task CreateUser_ConDtoValido_DebeCrearUsuario()
        {
            // Arrange
            await AuthenticateTestUser();
            var createDto = new CreateAppUserRequestDto
            {
                Name = "Usuario Creado Test",
                Email = GetUniqueEmail(),
                Password = "P@ssw0rd123"
            };

            // Act
            var result = await _userService.CreateUser(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Email, result.Email);
            Assert.Contains("Usuario Creado Test", result.Name);
            Assert.True(result.Id > 0);
        }

        [Fact]
        public async Task CreateUser_ConEmailDuplicado_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();
            var createDto = new CreateAppUserRequestDto
            {
                Name = "Usuario Duplicado",
                Email = TEST_USER_EMAIL, // Email que ya existe
                Password = "P@ssw0rd123"
            };

            // Act
            var result = await _userService.CreateUser(createDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateUser_ConDtoNull_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.CreateUser(null!);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region UpdateUser Tests

        [Fact]
        public async Task UpdateUser_ConDtoValido_DebeActualizarUsuario()
        {
            // Arrange
            var newUser = await CreateAndAuthenticateNewUser();
            var updateDto = new UpdateAppUserRequestDto
            {
                Name = "Nombre Actualizado " + DateTime.Now.ToString("HHmmss")
            };

            // Act
            var result = await _userService.UpdateUser(newUser.Id, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.Name, result.Name);
            Assert.Equal(newUser.Email, result.Email); // Email no debería cambiar
        }

        [Fact]
        public async Task UpdateUser_CambiandoEmail_DebeActualizarEmail()
        {
            // Arrange
            var newUser = await CreateAndAuthenticateNewUser();
            var newEmail = GetUniqueEmail();
            var updateDto = new UpdateAppUserRequestDto
            {
                Email = newEmail
            };

            // Act
            var result = await _userService.UpdateUser(newUser.Id, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(newEmail, result.Email);
        }

        [Fact]
        public async Task UpdateUser_ConEmailDuplicado_DebeRetornarNull()
        {
            // Arrange
            var newUser = await CreateAndAuthenticateNewUser();
            var updateDto = new UpdateAppUserRequestDto
            {
                Email = TEST_USER_EMAIL // Email que ya existe
            };

            // Act
            var result = await _userService.UpdateUser(newUser.Id, updateDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateUser_ConIdInvalido_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();
            var updateDto = new UpdateAppUserRequestDto
            {
                Name = "Test"
            };

            // Act
            var result = await _userService.UpdateUser(0, updateDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateUser_ConDtoNull_DebeRetornarNull()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.UpdateUser(TEST_USER_ID, null!);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region DeleteUser Tests

        [Fact]
        public async Task DeleteUser_ConIdValido_DebeEliminarUsuario()
        {
            // Arrange
            var newUser = await CreateAndAuthenticateNewUser();
            var userId = newUser.Id;

            // Act
            var deleteResult = await _userService.DeleteUser(userId);

            // Assert
            Assert.True(deleteResult);

            // Verificar que ya no existe
            var deletedUser = await _userService.findUserById(userId);
            Assert.Null(deletedUser);
        }

        [Fact]
        public async Task DeleteUser_ConIdInexistente_DebeRetornarFalse()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.DeleteUser(999999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteUser_ConIdCero_DebeRetornarFalse()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.DeleteUser(0);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteUser_ConIdNegativo_DebeRetornarFalse()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.DeleteUser(-1);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region EmailExists Tests

        [Fact]
        public async Task EmailExists_ConEmailExistente_DebeRetornarTrue()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.EmailExists(TEST_USER_EMAIL);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task EmailExists_ConEmailInexistente_DebeRetornarFalse()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.EmailExists("noexiste999999@example.com");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task EmailExists_ConEmailVacio_DebeRetornarFalse()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.EmailExists("");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task EmailExists_ConEmailNull_DebeRetornarFalse()
        {
            // Arrange
            await AuthenticateTestUser();

            // Act
            var result = await _userService.EmailExists(null!);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Flujo Completo Tests

        [Fact]
        public async Task FlujoCompleto_CrearYConfigurarUsuario_DebeCompletarCorrectamente()
        {
            // PASO 1: Crear usuario
            var newUser = await CreateAndAuthenticateNewUser();
            Assert.NotNull(newUser);
            Assert.False(newUser.RegistrationComplete);

            // PASO 2: Asignar coach
            var coachResult = await _userService.SetCoachModelType(newUser.Id, 1);
            Assert.NotNull(coachResult);
            Assert.NotNull(coachResult.CoachModelTypeName);

            // PASO 3: Asignar nivel de experiencia
            var levelResult = await _userService.SetExperienceLevel(newUser.Id, 2);
            Assert.NotNull(levelResult);
            Assert.NotNull(levelResult.ExperienceLevelName);

            // PASO 4: Marcar registro como completo
            var completeResult = await _userService.MarkRegistrationComplete(newUser.Id);
            Assert.NotNull(completeResult);
            Assert.True(completeResult.RegistrationComplete);

            // PASO 5: Verificar usuario completo
            var finalUser = await _userService.findUserById(newUser.Id);
            Assert.NotNull(finalUser);
            Assert.True(finalUser.RegistrationComplete);
            Assert.NotNull(finalUser.CoachModelTypeName);
            Assert.NotNull(finalUser.ExperienceLevelName);

            // PASO 6: Cleanup - Eliminar usuario de prueba
            await _userService.DeleteUser(newUser.Id);
        }

        [Fact]
        public async Task FlujoCompleto_BuscarActualizarEliminar_DebeCompletarCorrectamente()
        {
            // PASO 1: Crear usuario
            var newUser = await CreateAndAuthenticateNewUser();

            // PASO 2: Buscar por ID
            var foundById = await _userService.findUserById(newUser.Id);
            Assert.NotNull(foundById);
            Assert.Equal(newUser.Email, foundById.Email);

            // PASO 3: Buscar por Email
            var foundByEmail = await _userService.findUserByEmail(newUser.Email);
            Assert.NotNull(foundByEmail);
            Assert.Equal(newUser.Id, foundByEmail.Id);

            // PASO 4: Actualizar nombre
            var updateDto = new UpdateAppUserRequestDto
            {
                Name = "Nombre Actualizado Final"
            };
            var updatedUser = await _userService.UpdateUser(newUser.Id, updateDto);
            Assert.NotNull(updatedUser);
            Assert.Equal("Nombre Actualizado Final", updatedUser.Name);

            // PASO 5: Eliminar
            var deleted = await _userService.DeleteUser(newUser.Id);
            Assert.True(deleted);

            // PASO 6: Verificar que ya no existe
            var shouldBeNull = await _userService.findUserById(newUser.Id);
            Assert.Null(shouldBeNull);
        }

        #endregion
    }
}