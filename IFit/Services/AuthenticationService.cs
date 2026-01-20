using IFit.Models;
using IFit.Models.Dtos.AppUser.IFit.Models.Dtos.User;
using IFit.Models.Dtos.Auth;
using System.Diagnostics;

namespace IFit.Services
{
    /// <summary>
    /// Servicio para operaciones de autenticación (login, register, logout, refresh)
    /// Maneja la comunicación con el backend de autenticación y gestiona tokens localmente
    /// </summary>
    public class AuthenticationService
    {
        private readonly WebService _webService;

        public AuthenticationService(WebService webService)
        {
            _webService = webService ?? throw new ArgumentNullException(nameof(webService));
        }

        #region Métodos Principales de Autenticación

        /// <summary>
        /// Realiza el login del usuario con email y contraseña
        /// Guarda automáticamente los tokens en SecureStorage
        /// </summary>
        public async Task<AuthResponse?> LoginAsync(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    Debug.WriteLine("Email o password vacíos");
                    return null;
                }

                var request = new AuthRequestDto
                {
                    Username = email,
                    Password = password
                };

                var response = await _webService.PostAsync<AuthRequestDto, AuthResponse>(
                    "/auth/login",
                    request,
                    requiresAuth: false
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error en login: {response.ErrorMessage}");
                    return null;
                }

                // Guardar los tokens automáticamente
                if (response.Data != null)
                {
                    await _webService.SaveAuthenticationAsync(response.Data);
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en LoginAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Registra un nuevo usuario en el sistema
        /// Realiza login automático después del registro exitoso
        /// </summary>
        public async Task<AuthResponse?> RegisterAsync(string name, string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) ||
                    string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password))
                {
                    Debug.WriteLine("Datos de registro incompletos");
                    return null;
                }

                var request = new RegisterRequestDto
                {
                    Name = name,
                    Email = email,
                    Password = password
                };

                var response = await _webService.PostAsync<RegisterRequestDto, AuthResponse>(
                    "/auth/register",
                    request,
                    requiresAuth: false
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error en registro: {response.ErrorMessage}");
                    return null;
                }

                // Guardar los tokens automáticamente
                if (response.Data != null)
                {
                    await _webService.SaveAuthenticationAsync(response.Data);
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en RegisterAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Refresca los tokens de autenticación usando el refresh token actual
        /// </summary>
        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                var refreshToken = await GetRefreshTokenAsync();

                if (string.IsNullOrEmpty(refreshToken))
                {
                    Debug.WriteLine("No hay refresh token disponible");
                    return false;
                }

                var request = new RefreshTokenRequestDto
                {
                    RefreshToken = refreshToken
                };

                var response = await _webService.PostAsync<RefreshTokenRequestDto, AuthResponse>(
                    "/auth/refresh",
                    request,
                    requiresAuth: false
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error refrescando token: {response.ErrorMessage}");
                    return false;
                }

                // Guardar los nuevos tokens
                if (response.Data != null)
                {
                    await _webService.SaveAuthenticationAsync(response.Data);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en RefreshTokenAsync: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cierra la sesión del usuario
        /// Invalida el refresh token en el servidor y limpia tokens locales
        /// </summary>
        public async Task<bool> LogoutAsync()
        {
            try
            {
                var refreshToken = await GetRefreshTokenAsync();

                if (string.IsNullOrEmpty(refreshToken))
                {
                    Debug.WriteLine("No hay refresh token para invalidar");
                    await _webService.LogoutAsync();
                    return true;
                }

                var request = new RefreshTokenRequestDto
                {
                    RefreshToken = refreshToken
                };

                var response = await _webService.PostAsync<RefreshTokenRequestDto, LogoutResponseDto>(
                    "/auth/logout",
                    request,
                    requiresAuth: true
                );

                // Independientemente del resultado, limpiamos tokens locales
                await _webService.LogoutAsync();

                if (!response.Success)
                {
                    Debug.WriteLine($"Error en logout del servidor: {response.ErrorMessage}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en LogoutAsync: {ex.Message}");
                await _webService.LogoutAsync();
                return false;
            }
        }

        #endregion

        #region Métodos de Utilidad

        /// <summary>
        /// Verifica si hay una sesión activa
        /// </summary>
        public async Task<bool> IsAuthenticatedAsync()
        {
            return await _webService.IsAuthenticatedAsync();
        }

        /// <summary>
        /// Obtiene los datos del usuario actualmente autenticado
        /// </summary>
        public async Task<AppUserResponseDto?> GetCurrentUserAsync()
        {
            var user = await _webService.GetCurrentUserAsync();
            // Convertir AppUser a AppUserResponseDto si es necesario
            // O cambiar WebService para que devuelva AppUserResponseDto directamente
            return user != null ? ConvertToResponseDto(user) : null;
        }

        /// <summary>
        /// Obtiene el refresh token actual
        /// </summary>
        private async Task<string?> GetRefreshTokenAsync()
        {
            return await _webService.GetRefreshTokenAsync();
        }

        /// <summary>
        /// Convierte AppUser a AppUserResponseDto
        /// </summary>
        private AppUserResponseDto ConvertToResponseDto(AppUser user)
        {
            return new AppUserResponseDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CoachModelTypeName = user.CoachModelTypeName,
                ExperienceLevelName = user.ExperienceLevelName,
                RegistrationComplete = user.RegistrationComplete,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        #endregion
    }
}