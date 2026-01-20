using Xunit;
using Moq;
using IFit.Services;
using IFit.Models;
using System;
using System.Threading.Tasks;
using IFit.Models.Dtos.Auth;

namespace IFit.XUnit.Unit.Services
{
    /// <summary>
    /// Pruebas unitarias para TokenManager
    /// Versión con mock de SecureStorage - funciona sin dispositivo/emulador
    /// </summary>
    public class TokenManagerTests
    {
        private readonly Mock<ISecureStorageService> _mockSecureStorage;
        private readonly TokenManager _tokenManager;
        private readonly Dictionary<string, string> _storage; // Simula el almacenamiento

        public TokenManagerTests()
        {
            _mockSecureStorage = new Mock<ISecureStorageService>();
            _storage = new Dictionary<string, string>();

            // Configurar el mock para simular comportamiento de SecureStorage
            _mockSecureStorage
                .Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string key, string value) =>
                {
                    _storage[key] = value;
                    return Task.CompletedTask;
                });

            _mockSecureStorage
                .Setup(x => x.GetAsync(It.IsAny<string>()))
                .Returns((string key) =>
                {
                    return Task.FromResult(_storage.ContainsKey(key) ? _storage[key] : null);
                });

            _mockSecureStorage
                .Setup(x => x.Remove(It.IsAny<string>()))
                .Returns((string key) =>
                {
                    return _storage.Remove(key);
                });

            _tokenManager = new TokenManager(_mockSecureStorage.Object);
        }

        #region Tests de Guardado de Tokens

        [Fact]
        public async Task SaveAuthDataAsync_DebeGuardarTodosLosDatos()
        {
            // Arrange
            var authResponse = new AuthResponse
            {
                AccessToken = "test_access_token_123",
                RefreshToken = "test_refresh_token_456",
                ExpiresIn = 300,
                TokenType = "Bearer",
                AppUser = new AppUserResponseDto
                {
                    Id = 1,
                    Name = "Juan Test",
                    Email = "juan@test.com",
                    RoleName = "ROLE_USER",
                    RegistrationComplete = true,
                    Verified = true
                }
            };

            // Act
            await _tokenManager.SaveAuthDataAsync(authResponse);

            // Assert
            var accessToken = await _tokenManager.GetAccessTokenAsync();
            var refreshToken = await _tokenManager.GetRefreshTokenAsync();
            var userData = await _tokenManager.GetUserDataAsync();

            Assert.Equal("test_access_token_123", accessToken);
            Assert.Equal("test_refresh_token_456", refreshToken);
            Assert.NotNull(userData);
            Assert.Equal("Juan Test", userData.Name);
            Assert.Equal("juan@test.com", userData.Email);

            // Verificar que se llamó a SetAsync
            _mockSecureStorage.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeast(3));
        }

        [Fact]
        public async Task SaveAuthDataAsync_ConUsuarioNulo_DebeGuardarSoloTokens()
        {
            // Arrange
            var authResponse = new AuthResponse
            {
                AccessToken = "access_token",
                RefreshToken = "refresh_token",
                ExpiresIn = 300,
                TokenType = "Bearer",
                AppUser = null // Sin usuario
            };

            // Act
            await _tokenManager.SaveAuthDataAsync(authResponse);

            // Assert
            var accessToken = await _tokenManager.GetAccessTokenAsync();
            var refreshToken = await _tokenManager.GetRefreshTokenAsync();
            var userData = await _tokenManager.GetUserDataAsync();

            Assert.NotNull(accessToken);
            Assert.NotNull(refreshToken);
            Assert.Null(userData);
        }

        #endregion

        #region Tests de Recuperación de Tokens

        [Fact]
        public async Task GetAccessTokenAsync_SinTokenGuardado_DebeRetornarNull()
        {
            // Arrange - sin guardar ningún token

            // Act
            var token = await _tokenManager.GetAccessTokenAsync();

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public async Task GetRefreshTokenAsync_SinTokenGuardado_DebeRetornarNull()
        {
            // Arrange - sin guardar ningún token

            // Act
            var token = await _tokenManager.GetRefreshTokenAsync();

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public async Task GetUserDataAsync_SinDatosGuardados_DebeRetornarNull()
        {
            // Arrange - sin guardar datos

            // Act
            var userData = await _tokenManager.GetUserDataAsync();

            // Assert
            Assert.Null(userData);
        }

        #endregion

        #region Tests de Expiración de Tokens

        [Fact]
        public async Task IsTokenExpiredAsync_ConTokenValido_DebeRetornarFalse()
        {
            // Arrange - token que expira en 1 hora
            var authResponse = new AuthResponse
            {
                AccessToken = "valid_token",
                RefreshToken = "refresh_token",
                ExpiresIn = 3600, // 1 hora
                TokenType = "Bearer"
            };

            await _tokenManager.SaveAuthDataAsync(authResponse);

            // Act
            var isExpired = await _tokenManager.IsTokenExpiredAsync();

            // Assert
            Assert.False(isExpired);
        }

        [Fact]
        public async Task IsTokenExpiredAsync_ConTokenExpirado_DebeRetornarTrue()
        {
            // Arrange - token que ya expiró
            var authResponse = new AuthResponse
            {
                AccessToken = "expired_token",
                RefreshToken = "refresh_token",
                ExpiresIn = -3700, // Ya expirado
                TokenType = "Bearer"
            };

            await _tokenManager.SaveAuthDataAsync(authResponse);

            // Act
            var isExpired = await _tokenManager.IsTokenExpiredAsync();

            // Assert
            Assert.True(isExpired);
        }

        [Fact]
        public async Task IsTokenExpiredAsync_ConBufferPersonalizado_DebeConsiderarBuffer()
        {
            // Arrange - token que expira en 30 segundos
            var authResponse = new AuthResponse
            {
                AccessToken = "token",
                RefreshToken = "refresh",
                ExpiresIn = 30, // 30 segundos
                TokenType = "Bearer"
            };

            await _tokenManager.SaveAuthDataAsync(authResponse);

            // Act - buffer de 60 segundos
            var isExpiredWithBuffer = await _tokenManager.IsTokenExpiredAsync(bufferSeconds: 60);

            // Assert - debe considerarse expirado porque falta menos del buffer
            Assert.True(isExpiredWithBuffer);
        }

        [Fact]
        public async Task IsTokenExpiredAsync_ConBufferMenorQueExpiracion_NoDebeConsiderarExpirado()
        {
            // Arrange - token que expira en 120 segundos (2 minutos)
            var authResponse = new AuthResponse
            {
                AccessToken = "token",
                RefreshToken = "refresh",
                ExpiresIn = 120, // 120 segundos
                TokenType = "Bearer"
            };

            await _tokenManager.SaveAuthDataAsync(authResponse);

            // Act - buffer de 60 segundos (menor que el tiempo de expiración)
            var isExpiredWithBuffer = await _tokenManager.IsTokenExpiredAsync(bufferSeconds: 60);

            // Assert - NO debe considerarse expirado porque falta más del buffer (120 > 60)
            Assert.False(isExpiredWithBuffer);
        }

        [Fact]
        public async Task IsTokenExpiredAsync_SinToken_DebeRetornarTrue()
        {
            // Arrange - sin guardar token

            // Act
            var isExpired = await _tokenManager.IsTokenExpiredAsync();

            // Assert
            Assert.True(isExpired);
        }

        #endregion

        #region Tests de Sesión Activa

        [Fact]
        public async Task HasActiveSessionAsync_ConTokenGuardado_DebeRetornarTrue()
        {
            // Arrange
            var authResponse = new AuthResponse
            {
                AccessToken = "token",
                RefreshToken = "refresh",
                ExpiresIn = 300,
                TokenType = "Bearer"
            };

            await _tokenManager.SaveAuthDataAsync(authResponse);

            // Act
            var hasSession = await _tokenManager.HasActiveSessionAsync();

            // Assert
            Assert.True(hasSession);
        }

        [Fact]
        public async Task HasActiveSessionAsync_SinToken_DebeRetornarFalse()
        {
            // Arrange - sin guardar token

            // Act
            var hasSession = await _tokenManager.HasActiveSessionAsync();

            // Assert
            Assert.False(hasSession);
        }

        #endregion

        #region Tests de Limpieza de Datos

        [Fact]
        public async Task ClearAuthDataAsync_DebeLimpiarTodosLosDatos()
        {
            // Arrange - guardar datos primero
            var authResponse = new AuthResponse
            {
                AccessToken = "token",
                RefreshToken = "refresh",
                ExpiresIn = 300,
                TokenType = "Bearer",
                AppUser = new AppUserResponseDto
                {
                    Id = 1,
                    Name = "Test",
                    Email = "test@test.com"
                }
            };

            await _tokenManager.SaveAuthDataAsync(authResponse);

            // Verificar que se guardaron
            var hasSessionBefore = await _tokenManager.HasActiveSessionAsync();
            Assert.True(hasSessionBefore);

            // Act
            await _tokenManager.ClearAuthDataAsync();

            // Assert
            var accessToken = await _tokenManager.GetAccessTokenAsync();
            var refreshToken = await _tokenManager.GetRefreshTokenAsync();
            var userData = await _tokenManager.GetUserDataAsync();
            var hasSessionAfter = await _tokenManager.HasActiveSessionAsync();

            Assert.Null(accessToken);
            Assert.Null(refreshToken);
            Assert.Null(userData);
            Assert.False(hasSessionAfter);

            // Verificar que se llamó a Remove
            _mockSecureStorage.Verify(x => x.Remove(It.IsAny<string>()), Times.AtLeast(4));
        }

        [Fact]
        public async Task ClearAuthDataAsync_LlamadaMultiple_NoDebeGenerarError()
        {
            // Arrange
            await _tokenManager.ClearAuthDataAsync();

            // Act & Assert - no debería lanzar excepción
            await _tokenManager.ClearAuthDataAsync();
            await _tokenManager.ClearAuthDataAsync();
        }

        #endregion

        #region Tests de Actualización de Tokens

        [Fact]
        public async Task SaveAuthDataAsync_ActualizarTokensExistentes_DebeReemplazar()
        {
            // Arrange - guardar primeros tokens
            var firstAuth = new AuthResponse
            {
                AccessToken = "first_access_token",
                RefreshToken = "first_refresh_token",
                ExpiresIn = 300,
                TokenType = "Bearer"
            };

            await _tokenManager.SaveAuthDataAsync(firstAuth);

            // Act - guardar nuevos tokens
            var secondAuth = new AuthResponse
            {
                AccessToken = "second_access_token",
                RefreshToken = "second_refresh_token",
                ExpiresIn = 600,
                TokenType = "Bearer"
            };

            await _tokenManager.SaveAuthDataAsync(secondAuth);

            // Assert
            var accessToken = await _tokenManager.GetAccessTokenAsync();
            var refreshToken = await _tokenManager.GetRefreshTokenAsync();

            Assert.Equal("second_access_token", accessToken);
            Assert.Equal("second_refresh_token", refreshToken);
        }

        #endregion

        #region Tests de Persistencia de Datos del Usuario

        [Fact]
        public async Task GetUserDataAsync_DebeDeserializarCorrectamente()
        {
            // Arrange
            var expectedUser = new AppUserResponseDto
            {
                Id = 123,
                Name = "Juan García",
                Email = "juan.garcia@example.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                RoleName = "ROLE_ADMIN",
                CoachModelTypeName = "Ronnie",
                ExperienceLevelName = "Advanced",
                RegistrationComplete = true,
                Verified = true
            };

            var authResponse = new AuthResponse
            {
                AccessToken = "token",
                RefreshToken = "refresh",
                ExpiresIn = 300,
                AppUser = expectedUser
            };

            await _tokenManager.SaveAuthDataAsync(authResponse);

            // Act
            var actualUser = await _tokenManager.GetUserDataAsync();

            // Assert
            Assert.NotNull(actualUser);
            Assert.Equal(expectedUser.Id, actualUser.Id);
            Assert.Equal(expectedUser.Name, actualUser.Name);
            Assert.Equal(expectedUser.Email, actualUser.Email);
            Assert.Equal(expectedUser.RoleName, actualUser.RoleName);
            Assert.Equal(expectedUser.CoachModelTypeName, actualUser.CoachModelTypeName);
            Assert.Equal(expectedUser.ExperienceLevelName, actualUser.ExperienceLevelName);
            Assert.Equal(expectedUser.RegistrationComplete, actualUser.RegistrationComplete);
            Assert.Equal(expectedUser.Verified, actualUser.Verified);
        }

        #endregion

        #region Tests de Casos Extremos

        [Fact]
        public async Task SaveAuthDataAsync_ConTokenMuyLargo_DebeGuardarCorrectamente()
        {
            // Arrange - simular JWT muy largo
            var longToken = new string('a', 5000); // 5000 caracteres

            var authResponse = new AuthResponse
            {
                AccessToken = longToken,
                RefreshToken = longToken,
                ExpiresIn = 300,
                TokenType = "Bearer"
            };

            // Act
            await _tokenManager.SaveAuthDataAsync(authResponse);

            // Assert
            var retrievedToken = await _tokenManager.GetAccessTokenAsync();
            Assert.Equal(longToken, retrievedToken);
        }

        [Fact]
        public async Task SaveAuthDataAsync_ConCaracteresEspeciales_DebeGuardarCorrectamente()
        {
            // Arrange
            var specialCharsUser = new AppUserResponseDto
            {
                Id = 1,
                Name = "José María Ñoño",
                Email = "josé@ñoño.com",
                RoleName = "ROLE_USER"
            };

            var authResponse = new AuthResponse
            {
                AccessToken = "token",
                RefreshToken = "refresh",
                ExpiresIn = 300,
                AppUser = specialCharsUser
            };

            // Act
            await _tokenManager.SaveAuthDataAsync(authResponse);

            // Assert
            var userData = await _tokenManager.GetUserDataAsync();
            Assert.NotNull(userData);
            Assert.Equal("José María Ñoño", userData.Name);
            Assert.Equal("josé@ñoño.com", userData.Email);
        }

        #endregion
    }
}