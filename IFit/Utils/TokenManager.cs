using IFit.Models;
using System.Text.Json;
using System.Globalization;

namespace IFit.Services
{
    /// <summary>
    /// Servicio para gestionar tokens de autenticación (acceso y refresco)
    /// Versión con inyección de dependencias para permitir pruebas unitarias
    /// </summary>
    public class TokenManager
    {
        private const string ACCESS_TOKEN_KEY = "ifit_access_token";
        private const string REFRESH_TOKEN_KEY = "ifit_refresh_token";
        private const string TOKEN_EXPIRY_KEY = "ifit_token_expiry";
        private const string USER_DATA_KEY = "ifit_user_data";

        private readonly ISecureStorageService _secureStorage;

        /// <summary>
        /// Constructor con inyección de dependencias
        /// </summary>
        /// <param name="secureStorage">Servicio de almacenamiento seguro (inyectado)</param>
        public TokenManager(ISecureStorageService secureStorage)
        {
            _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
        }

        /// <summary>
        /// Constructor por defecto para compatibilidad hacia atrás
        /// Usa la implementación real de SecureStorage
        /// </summary>
        public TokenManager() : this(new SecureStorageService())
        {
        }

        /// <summary>
        /// Guarda los tokens de autenticación de forma segura
        /// </summary>
        public async Task SaveAuthDataAsync(AuthResponse authResponse)
        {
            try
            {
                await _secureStorage.SetAsync(ACCESS_TOKEN_KEY, authResponse.AccessToken);
                await _secureStorage.SetAsync(REFRESH_TOKEN_KEY, authResponse.RefreshToken);

                // Calcular y guardar el momento de expiración
                var expiryTime = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn);
                await _secureStorage.SetAsync(TOKEN_EXPIRY_KEY, expiryTime.ToString("o"));

                // Guardar información del usuario
                if (authResponse.AppUser != null)
                {
                    var userJson = JsonSerializer.Serialize(authResponse.AppUser);
                    await _secureStorage.SetAsync(USER_DATA_KEY, userJson);
                }
            }
            catch (Exception ex)
            {
                // Log error - en producción deberías usar un sistema de logging
                System.Diagnostics.Debug.WriteLine($"Error guardando tokens: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene el token de acceso almacenado
        /// </summary>
        public async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                return await _secureStorage.GetAsync(ACCESS_TOKEN_KEY);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo access token: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene el token de refresco almacenado
        /// </summary>
        public async Task<string?> GetRefreshTokenAsync()
        {
            try
            {
                return await _secureStorage.GetAsync(REFRESH_TOKEN_KEY);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo refresh token: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Verifica si el token de acceso ha expirado o está próximo a expirar
        /// </summary>
        /// <param name="bufferSeconds">Segundos de margen antes de la expiración (por defecto 60s)</param>
        public async Task<bool> IsTokenExpiredAsync(int bufferSeconds = 60)
        {
            try
            {
                var expiryStr = await _secureStorage.GetAsync(TOKEN_EXPIRY_KEY);

                if (string.IsNullOrEmpty(expiryStr))
                    return true;

                // Parsear preservando la zona horaria original (RoundtripKind)
                if (DateTime.TryParse(expiryStr,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime expiryTime))
                {
                    // Asegurar que estamos comparando en UTC
                    DateTime expiryTimeUtc = expiryTime.Kind == DateTimeKind.Utc
                        ? expiryTime
                        : expiryTime.ToUniversalTime();

                    DateTime nowWithBuffer = DateTime.UtcNow.AddSeconds(bufferSeconds);

                    // Consideramos expirado si falta menos del buffer de tiempo
                    return nowWithBuffer >= expiryTimeUtc;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error verificando expiración: {ex.Message}");
                return true; // Por seguridad, asumimos que está expirado
            }
        }

        /// <summary>
        /// Obtiene la información del usuario almacenada
        /// </summary>
        public async Task<AppUser?> GetUserDataAsync()
        {
            try
            {
                var userJson = await _secureStorage.GetAsync(USER_DATA_KEY);

                if (string.IsNullOrEmpty(userJson))
                    return null;

                return JsonSerializer.Deserialize<AppUser>(userJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo datos de usuario: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Verifica si hay una sesión activa (tokens guardados)
        /// </summary>
        public async Task<bool> HasActiveSessionAsync()
        {
            var accessToken = await GetAccessTokenAsync();
            return !string.IsNullOrEmpty(accessToken);
        }

        /// <summary>
        /// Limpia todos los tokens y datos de usuario almacenados
        /// </summary>
        public async Task ClearAuthDataAsync()
        {
            try
            {
                _secureStorage.Remove(ACCESS_TOKEN_KEY);
                _secureStorage.Remove(REFRESH_TOKEN_KEY);
                _secureStorage.Remove(TOKEN_EXPIRY_KEY);
                _secureStorage.Remove(USER_DATA_KEY);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error limpiando datos de autenticación: {ex.Message}");
                throw;
            }
        }
    }
}