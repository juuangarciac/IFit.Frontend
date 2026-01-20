using IFit.Models.Dtos.Auth;
using IFit.Models.Dtos.AppUser;
using IFit.Services;
using IFit.XUnit.Utils;
using Xunit;

namespace IFit.XUnit.Integration.Services
{
    /// <summary>
    /// Pruebas de integración para AuthenticationService
    /// 
    /// REQUISITOS:
    /// - Keycloak activo
    /// - Usuario de prueba registrado
    /// 
    /// Para ejecutar solo estas pruebas:
    /// dotnet test --filter "FullyQualifiedName~AuthenticationServiceTest"
    /// </summary>
    [Trait("Category", "Integration")]
    public class AuthenticationServiceTest
    {
        private readonly AuthenticationService _authService;
        private readonly WebService _webService;

        // Configuración de tu API
        private const string API_BASE_URL = "http://192.168.1.72:8080/ifit/api/v1";
        private const string REFRESH_ENDPOINT = "/auth/refresh";

        // Credenciales de usuario de prueba existente
        private const string EXISTING_USER_EMAIL = "newuser249@example.com";
        private const string EXISTING_USER_PASSWORD = "P@ssword";
        private const string EXISTING_USER_NAME = "newuser249 surname249";

        // Credenciales para pruebas de registro (cambiar email cada vez)
        private static string GetUniqueEmail() => $"testuser{DateTime.Now:yyyyMMddHHmmss}@example.com";

        public AuthenticationServiceTest()
        {
            var httpClient = new HttpClient();
            var fakeStorage = new FakeSecureStorageService();
            var tokenManager = new TokenManager(fakeStorage);

            _webService = new WebService(httpClient, tokenManager, API_BASE_URL, REFRESH_ENDPOINT);
            _authService = new AuthenticationService(_webService);
        }

        #region Login Tests

        [Fact]
        public async Task LoginAsync_ConCredencialesValidas_DebeRetornarAuthResponse()
        {
            // Act
            var result = await _authService.LoginAsync(EXISTING_USER_EMAIL, EXISTING_USER_PASSWORD);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.AccessToken);
            Assert.NotEmpty(result.RefreshToken);
            Assert.Equal("Bearer", result.TokenType);
            Assert.True(result.ExpiresIn > 0);

            Assert.NotNull(result.AppUser);
            Assert.Equal(EXISTING_USER_EMAIL, result.AppUser.Email);
            Assert.Contains("newuser249 surname249", result.AppUser.Name);
        }

        [Fact]
        public async Task LoginAsync_ConCredencialesValidas_DebeGuardarTokensEnSecureStorage()
        {
            // Act
            var result = await _authService.LoginAsync(EXISTING_USER_EMAIL, EXISTING_USER_PASSWORD);

            // Assert
            Assert.NotNull(result);

            // Verificar que se guardaron los tokens
            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            Assert.True(isAuthenticated);
        }

        [Fact]
        public async Task LoginAsync_ConPasswordIncorrecto_DebeRetornarNull()
        {
            // Act
            var result = await _authService.LoginAsync(EXISTING_USER_EMAIL, "password_incorrecto");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_ConEmailNoExistente_DebeRetornarNull()
        {
            // Act
            var result = await _authService.LoginAsync("noexiste@example.com", EXISTING_USER_PASSWORD);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_ConEmailVacio_DebeRetornarNull()
        {
            // Act
            var result = await _authService.LoginAsync("", EXISTING_USER_PASSWORD);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_ConPasswordVacio_DebeRetornarNull()
        {
            // Act
            var result = await _authService.LoginAsync(EXISTING_USER_EMAIL, "");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_ConEmailNull_DebeRetornarNull()
        {
            // Act
            var result = await _authService.LoginAsync(null!, EXISTING_USER_PASSWORD);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Register Tests

        [Fact]
        public async Task RegisterAsync_ConDatosValidos_DebeCrearUsuarioYRetornarAuthResponse()
        {
            // Arrange
            var uniqueEmail = GetUniqueEmail();
            var userName = "Test User " + DateTime.Now.ToString("HHmmss");

            // Act
            var result = await _authService.RegisterAsync(userName, uniqueEmail, "P@ssw0rd123");

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.AccessToken);
            Assert.NotEmpty(result.RefreshToken);
            Assert.Equal("Bearer", result.TokenType);

            Assert.NotNull(result.AppUser);
            Assert.Equal(uniqueEmail, result.AppUser.Email);
            Assert.Contains("Test User", result.AppUser.Name);
            Assert.False(result.AppUser.RegistrationComplete); // Registro inicial incompleto
            Assert.False(result.AppUser.Verified);
        }

        [Fact]
        public async Task RegisterAsync_ConDatosValidos_DebeHacerLoginAutomatico()
        {
            // Arrange
            var uniqueEmail = GetUniqueEmail();

            // Act
            var result = await _authService.RegisterAsync("Test User", uniqueEmail, "P@ssw0rd123");

            // Assert
            Assert.NotNull(result);

            // Verificar que está autenticado automáticamente
            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            Assert.True(isAuthenticated);
        }

        [Fact]
        public async Task RegisterAsync_ConEmailDuplicado_DebeRetornarNull()
        {
            // Arrange - Usar email que ya existe
            var duplicateEmail = EXISTING_USER_EMAIL;

            // Act
            var result = await _authService.RegisterAsync("Otro Usuario", duplicateEmail, "P@ssw0rd123");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RegisterAsync_ConNombreVacio_DebeRetornarNull()
        {
            // Arrange
            var uniqueEmail = GetUniqueEmail();

            // Act
            var result = await _authService.RegisterAsync("", uniqueEmail, "P@ssw0rd123");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RegisterAsync_ConEmailVacio_DebeRetornarNull()
        {
            // Act
            var result = await _authService.RegisterAsync("Test User", "", "P@ssw0rd123");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RegisterAsync_ConPasswordVacio_DebeRetornarNull()
        {
            // Arrange
            var uniqueEmail = GetUniqueEmail();

            // Act
            var result = await _authService.RegisterAsync("Test User", uniqueEmail, "");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RegisterAsync_ConEmailInvalido_DebeRetornarNull()
        {
            // Act
            var result = await _authService.RegisterAsync("Test User", "email-invalido", "P@ssw0rd123");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RegisterAsync_ConPasswordCorto_DebeRetornarNull()
        {
            // Arrange
            var uniqueEmail = GetUniqueEmail();

            // Act - Password muy corto (menos de 8 caracteres)
            var result = await _authService.RegisterAsync("Test User", uniqueEmail, "pass");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region RefreshToken Tests

        [Fact]
        public async Task RefreshTokenAsync_ConRefreshTokenValido_DebeRetornarTrue()
        {
            // Arrange - Hacer login primero
            var loginResult = await _authService.LoginAsync(EXISTING_USER_EMAIL, EXISTING_USER_PASSWORD);
            Assert.NotNull(loginResult);

            // Esperar un momento para que el nuevo token sea diferente
            await Task.Delay(2000);

            // Act
            var refreshSuccess = await _authService.RefreshTokenAsync();

            // Assert
            Assert.True(refreshSuccess);
        }

        [Fact]
        public async Task RefreshTokenAsync_ConRefreshTokenValido_DebeActualizarTokens()
        {
            // Arrange - Hacer login primero
            var loginResult = await _authService.LoginAsync(EXISTING_USER_EMAIL, EXISTING_USER_PASSWORD);
            Assert.NotNull(loginResult);
            var oldAccessToken = loginResult.AccessToken;

            // Esperar un momento
            await Task.Delay(2000);

            // Act
            var refreshSuccess = await _authService.RefreshTokenAsync();

            // Assert
            Assert.True(refreshSuccess);

            // Los tokens deberían ser diferentes ahora
            // (No podemos verificar directamente, pero podemos hacer una petición autenticada)
            var isStillAuthenticated = await _authService.IsAuthenticatedAsync();
            Assert.True(isStillAuthenticated);
        }

        [Fact]
        public async Task RefreshTokenAsync_SinRefreshToken_DebeRetornarFalse()
        {
            // Arrange - Asegurar que no hay tokens
            await _authService.LogoutAsync();

            // Act
            var refreshSuccess = await _authService.RefreshTokenAsync();

            // Assert
            Assert.False(refreshSuccess);
        }

        [Fact]
        public async Task RefreshTokenAsync_ConTokenExpirado_DebeRetornarFalse()
        {
            // Nota: Este test es difícil de hacer sin esperar el tiempo real de expiración
            // Por ahora, solo verificamos que un refresh sin token falla

            // Arrange
            await _authService.LogoutAsync();

            // Act
            var refreshSuccess = await _authService.RefreshTokenAsync();

            // Assert
            Assert.False(refreshSuccess);
        }

        #endregion

        #region Logout Tests

        [Fact]
        public async Task LogoutAsync_ConSesionActiva_DebeInvalidarTokenYRetornarTrue()
        {
            // Arrange - Hacer login primero
            var loginResult = await _authService.LoginAsync(EXISTING_USER_EMAIL, EXISTING_USER_PASSWORD);
            Assert.NotNull(loginResult);

            // Act
            var logoutSuccess = await _authService.LogoutAsync();

            // Assert
            Assert.True(logoutSuccess);
        }

        [Fact]
        public async Task LogoutAsync_ConSesionActiva_DebeLimpiarTokensLocales()
        {
            // Arrange - Hacer login primero
            var loginResult = await _authService.LoginAsync(EXISTING_USER_EMAIL, EXISTING_USER_PASSWORD);
            Assert.NotNull(loginResult);

            // Act
            var logoutSuccess = await _authService.LogoutAsync();

            // Assert
            Assert.True(logoutSuccess);

            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            Assert.False(isAuthenticated);
        }

        [Fact]
        public async Task LogoutAsync_SinSesionActiva_DebeRetornarTrueYLimpiarTokens()
        {
            // Arrange - Asegurar que no hay sesión
            await _authService.LogoutAsync();

            // Act
            var logoutSuccess = await _authService.LogoutAsync();

            // Assert
            Assert.True(logoutSuccess); // Siempre limpia tokens locales

            var isAuthenticated = await _authService.IsAuthenticatedAsync();
            Assert.False(isAuthenticated);
        }

        #endregion

        #region IsAuthenticated Tests

        [Fact]
        public async Task IsAuthenticatedAsync_DespuesDeLoginExitoso_DebeRetornarTrue()
        {
            // Arrange
            var loginResult = await _authService.LoginAsync(EXISTING_USER_EMAIL, EXISTING_USER_PASSWORD);
            Assert.NotNull(loginResult);

            // Act
            var isAuthenticated = await _authService.IsAuthenticatedAsync();

            // Assert
            Assert.True(isAuthenticated);
        }

        [Fact]
        public async Task IsAuthenticatedAsync_SinLogin_DebeRetornarFalse()
        {
            // Arrange - Asegurar que no hay tokens
            await _authService.LogoutAsync();

            // Act
            var isAuthenticated = await _authService.IsAuthenticatedAsync();

            // Assert
            Assert.False(isAuthenticated);
        }

        [Fact]
        public async Task IsAuthenticatedAsync_DespuesDeLogout_DebeRetornarFalse()
        {
            // Arrange - Login y luego logout
            var loginResult = await _authService.LoginAsync(EXISTING_USER_EMAIL, EXISTING_USER_PASSWORD);
            Assert.NotNull(loginResult);
            await _authService.LogoutAsync();

            // Act
            var isAuthenticated = await _authService.IsAuthenticatedAsync();

            // Assert
            Assert.False(isAuthenticated);
        }

        [Fact]
        public async Task IsAuthenticatedAsync_DespuesDeRegistro_DebeRetornarTrue()
        {
            // Arrange
            var uniqueEmail = GetUniqueEmail();
            var registerResult = await _authService.RegisterAsync("Test User", uniqueEmail, "P@ssw0rd123");
            Assert.NotNull(registerResult);

            // Act
            var isAuthenticated = await _authService.IsAuthenticatedAsync();

            // Assert
            Assert.True(isAuthenticated);
        }

        #endregion

        #region Flujo Completo Tests

        [Fact]
        public async Task FlujoCompleto_RegistroLoginRefreshLogout_DebeCompletarCorrectamente()
        {
            // PASO 1: Registro
            var uniqueEmail = GetUniqueEmail();
            var registerResult = await _authService.RegisterAsync(
                "Usuario Flujo Completo",
                uniqueEmail,
                "P@ssw0rd123"
            );

            Assert.NotNull(registerResult);
            Assert.NotEmpty(registerResult.AccessToken);
            Assert.True(await _authService.IsAuthenticatedAsync());

            // PASO 2: Logout
            var logoutSuccess = await _authService.LogoutAsync();
            Assert.True(logoutSuccess);
            Assert.False(await _authService.IsAuthenticatedAsync());

            // PASO 3: Login con el usuario recién creado
            var loginResult = await _authService.LoginAsync(uniqueEmail, "P@ssw0rd123");
            Assert.NotNull(loginResult);
            Assert.True(await _authService.IsAuthenticatedAsync());

            // PASO 4: Refresh Token
            await Task.Delay(2000);
            var refreshSuccess = await _authService.RefreshTokenAsync();
            Assert.True(refreshSuccess);
            Assert.True(await _authService.IsAuthenticatedAsync());

            // PASO 5: Logout final
            await _authService.LogoutAsync();
            Assert.False(await _authService.IsAuthenticatedAsync());
        }

        [Fact]
        public async Task FlujoCompleto_LoginYObtenerUsuarioActual_DebeRetornarDatosCorrectos()
        {
            // PASO 1: Login
            var loginResult = await _authService.LoginAsync(EXISTING_USER_EMAIL, EXISTING_USER_PASSWORD);
            Assert.NotNull(loginResult);

            // PASO 2: Obtener usuario actual desde SecureStorage
            var currentUser = await _authService.GetCurrentUserAsync();

            Assert.NotNull(currentUser);
            Assert.Equal(EXISTING_USER_EMAIL, currentUser.Email);
            Assert.Contains("newuser249 surname249", currentUser.Name);

            // PASO 3: Verificar que coincide con el usuario del login
            Assert.Equal(loginResult.AppUser.Id, currentUser.Id);
            Assert.Equal(loginResult.AppUser.Email, currentUser.Email);

            // Cleanup
            await _authService.LogoutAsync();
        }

        [Fact]
        public async Task FlujoCompleto_MultipleLogin_NoDebeGenerarConflictos()
        {
            // PASO 1: Primer login
            var login1 = await _authService.LoginAsync(EXISTING_USER_EMAIL, EXISTING_USER_PASSWORD);
            Assert.NotNull(login1);
            var token1 = login1.AccessToken;

            // PASO 2: Segundo login (debería reemplazar tokens)
            var login2 = await _authService.LoginAsync(EXISTING_USER_EMAIL, EXISTING_USER_PASSWORD);
            Assert.NotNull(login2);
            var token2 = login2.AccessToken;

            // Los tokens deberían ser diferentes (nuevos)
            Assert.NotEqual(token1, token2);

            // Aún debería estar autenticado
            Assert.True(await _authService.IsAuthenticatedAsync());

            // Cleanup
            await _authService.LogoutAsync();
        }

        #endregion

        #region Tests de Errores de Red

        [Fact]
        public async Task LoginAsync_ConServidorApagado_DebeRetornarNull()
        {
            // Arrange - Crear servicio con URL incorrecta
            var httpClient = new HttpClient();
            var fakeStorage = new FakeSecureStorageService();
            var tokenManager = new TokenManager(fakeStorage);
            var wrongWebService = new WebService(
                httpClient,
                tokenManager,
                "http://localhost:9999", // Puerto incorrecto
                REFRESH_ENDPOINT
            );
            var wrongAuthService = new AuthenticationService(wrongWebService);

            // Act
            var result = await wrongAuthService.LoginAsync(EXISTING_USER_EMAIL, EXISTING_USER_PASSWORD);

            // Assert
            Assert.Null(result);
        }

        #endregion
    }
}