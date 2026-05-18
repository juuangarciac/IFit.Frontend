using IFit.Models;
using System.Text.Json;
using System.Globalization;
using IFit.Models.Dtos.Auth;

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

        // In-memory cache — avoids hitting SecureStorage (slow on Android Keystore) on every request
        private string? _cachedAccessToken;
        private string? _cachedRefreshToken;
        private DateTime _cachedExpiry = DateTime.MinValue;

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
        /// Guarda los tokens de autenticación de forma segura y actualiza la caché en memoria
        /// </summary>
        public async Task SaveAuthDataAsync(AuthResponse authResponse)
        {
            try
            {
                // Update in-memory cache first (instant access next time)
                _cachedAccessToken = authResponse.AccessToken;
                _cachedRefreshToken = authResponse.RefreshToken;
                _cachedExpiry = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn ?? 0);

                if (!string.IsNullOrEmpty(authResponse.AccessToken))
                    await _secureStorage.SetAsync(ACCESS_TOKEN_KEY, authResponse.AccessToken);

                if (!string.IsNullOrEmpty(authResponse.RefreshToken))
                    await _secureStorage.SetAsync(REFRESH_TOKEN_KEY, authResponse.RefreshToken);

                // Calcular y guardar el momento de expiración
                await _secureStorage.SetAsync(TOKEN_EXPIRY_KEY, _cachedExpiry.ToString("o"));

                // Guardar información del usuario
                if (authResponse.AppUser != null)
                {
                    var userJson = JsonSerializer.Serialize(authResponse.AppUser);
                    await _secureStorage.SetAsync(USER_DATA_KEY, userJson);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error guardando tokens: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene el token de acceso — desde caché en memoria si está disponible
        /// </summary>
        public async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                if (_cachedAccessToken != null)
                    return _cachedAccessToken;

                _cachedAccessToken = await _secureStorage.GetAsync(ACCESS_TOKEN_KEY);
                return _cachedAccessToken;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo access token: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene el token de refresco — desde caché en memoria si está disponible
        /// </summary>
        public async Task<string?> GetRefreshTokenAsync()
        {
            try
            {
                if (_cachedRefreshToken != null)
                    return _cachedRefreshToken;

                _cachedRefreshToken = await _secureStorage.GetAsync(REFRESH_TOKEN_KEY);
                return _cachedRefreshToken;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error obteniendo refresh token: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Verifica si el token ha expirado — usa caché en memoria si está disponible
        /// </summary>
        /// <param name="bufferSeconds">Segundos de margen antes de la expiración (por defecto 60s)</param>
        public async Task<bool> IsTokenExpiredAsync(int bufferSeconds = 60)
        {
            try
            {
                // Use in-memory expiry if available (avoids SecureStorage read)
                if (_cachedExpiry != DateTime.MinValue)
                    return DateTime.UtcNow.AddSeconds(bufferSeconds) >= _cachedExpiry;

                var expiryStr = await _secureStorage.GetAsync(TOKEN_EXPIRY_KEY);

                if (string.IsNullOrEmpty(expiryStr))
                    return true;

                // Parsear preservando la zona horaria original (RoundtripKind)
                if (DateTime.TryParse(expiryStr,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime expiryTime))
                {
                    _cachedExpiry = expiryTime.Kind == DateTimeKind.Utc
                        ? expiryTime
                        : expiryTime.ToUniversalTime();

                    return DateTime.UtcNow.AddSeconds(bufferSeconds) >= _cachedExpiry;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error verificando expiración: {ex.Message}");
                return true;
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
        /// Limpia todos los tokens, datos de usuario y la caché en memoria
        /// </summary>
        public async Task ClearAuthDataAsync()
        {
            try
            {
                // Clear in-memory cache
                _cachedAccessToken = null;
                _cachedRefreshToken = null;
                _cachedExpiry = DateTime.MinValue;

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