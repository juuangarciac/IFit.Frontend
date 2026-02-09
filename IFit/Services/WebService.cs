using IFit.Models;
using IFit.Models.Dtos.Auth;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IFit.Services
{
    /// <summary>
    /// Servicio centralizado para todas las peticiones HTTP a la API de iFit
    /// Gestiona automáticamente la autenticación con tokens JWT y su refresco
    /// </summary>
    public class WebService
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClient _aiHttpClient;
        private readonly TokenManager _tokenManager;
        private readonly string _baseUrl;
        private readonly string _refreshEndpoint;

        // Opciones de serialización JSON reutilizables
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        /// <summary>
        /// Constructor del WebService
        /// </summary>
        /// <param name="httpClient">Cliente HTTP (usar AppSettings._HttpClient o inyección de dependencias)</param>
        /// <param name="baseUrl">URL base de la API (ej: "http://localhost:8080/ifit/api/v1")</param>
        /// <param name="refreshEndpoint">Endpoint para refrescar tokens (ej: "/auth/refresh")</param>
        public WebService(HttpClient httpClient, string baseUrl, string refreshEndpoint = "/auth/refresh")
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            _aiHttpClient = new HttpClient();
            _aiHttpClient.Timeout = TimeSpan.FromSeconds(1800); // Timeout largo para peticiones de AI

            _tokenManager = new TokenManager();
            _baseUrl = baseUrl.TrimEnd('/');
            _refreshEndpoint = refreshEndpoint;
        }

        
        /// <summary>
        /// Constructor del WebService
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="tokenManager"></param>
        /// <param name="baseUrl"></param>
        /// <param name="refreshEndpoint"></param>
        public WebService(HttpClient httpClient, TokenManager tokenManager, string baseUrl, string refreshEndpoint)
        {
            _httpClient = httpClient;
            _tokenManager = tokenManager; // ← Usa el TokenManager que le pases
            _baseUrl = baseUrl;
            _refreshEndpoint = refreshEndpoint;
        }

        #region Métodos HTTP Públicos

        /// <summary>
        /// Realiza una petición GET
        /// </summary>
        /// <typeparam name="T">Tipo de objeto esperado en la respuesta</typeparam>
        /// <param name="endpoint">Endpoint relativo (ej: "/users/profile")</param>
        /// <param name="requiresAuth">Si requiere autenticación (true por defecto)</param>
        /// <returns>Respuesta encapsulada con el objeto deserializado</returns>
        public async Task<ApiResponse<T>> GetAsync<T>(string endpoint, bool requiresAuth = true)
        {
            return await SendRequestAsync<T>(
                HttpMethod.Get,
                endpoint,
                null,
                requiresAuth
            );
        }

        /// <summary>
        /// Realiza una petición POST
        /// </summary>
        /// <typeparam name="TRequest">Tipo del objeto a enviar</typeparam>
        /// <typeparam name="TResponse">Tipo del objeto esperado en la respuesta</typeparam>
        /// <param name="endpoint">Endpoint relativo</param>
        /// <param name="data">Datos a enviar en el body</param>
        /// <param name="requiresAuth">Si requiere autenticación (true por defecto)</param>
        public async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
            string endpoint,
            TRequest data,
            bool requiresAuth = true)
        {
            return await SendRequestAsync<TResponse>(
                HttpMethod.Post,
                endpoint,
                data,
                requiresAuth
            );
        }

        /// <summary>
        /// Realiza una petición PUT
        /// </summary>
        /// <typeparam name="TRequest">Tipo del objeto a enviar</typeparam>
        /// <typeparam name="TResponse">Tipo del objeto esperado en la respuesta</typeparam>
        /// <param name="endpoint">Endpoint relativo</param>
        /// <param name="data">Datos a enviar en el body</param>
        /// <param name="requiresAuth">Si requiere autenticación (true por defecto)</param>
        public async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(
            string endpoint,
            TRequest data,
            bool requiresAuth = true)
        {
            return await SendRequestAsync<TResponse>(
                HttpMethod.Put,
                endpoint,
                data,
                requiresAuth
            );
        }

        /// <summary>
        /// Realiza una petición PATCH
        /// </summary>
        /// <typeparam name="TRequest">Tipo del objeto a enviar</typeparam>
        /// <typeparam name="TResponse">Tipo del objeto esperado en la respuesta</typeparam>
        /// <param name="endpoint">Endpoint relativo</param>
        /// <param name="data">Datos a enviar en el body</param>
        /// <param name="requiresAuth">Si requiere autenticación (true por defecto)</param>
        public async Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(
            string endpoint,
            TRequest data,
            bool requiresAuth = true)
        {
            return await SendRequestAsync<TResponse>(
                HttpMethod.Patch,
                endpoint,
                data,
                requiresAuth
            );
        }

        /// <summary>
        /// Realiza una petición DELETE
        /// </summary>
        /// <typeparam name="T">Tipo del objeto esperado en la respuesta (usar 'object' si no hay respuesta)</typeparam>
        /// <param name="endpoint">Endpoint relativo</param>
        /// <param name="requiresAuth">Si requiere autenticación (true por defecto)</param>
        public async Task<ApiResponse<T>> DeleteAsync<T>(string endpoint, bool requiresAuth = true)
        {
            return await SendRequestAsync<T>(
                HttpMethod.Delete,
                endpoint,
                null,
                requiresAuth
            );
        }

        #endregion

        #region Métodos de Autenticación

        /// <summary>
        /// Guarda los datos de autenticación (llamar después de login/register)
        /// </summary>
        public async Task SaveAuthenticationAsync(AuthResponse authResponse)
        {
            await _tokenManager.SaveAuthDataAsync(authResponse);
        }

        /// <summary>
        /// Cierra la sesión del usuario (limpia tokens)
        /// </summary>
        public async Task LogoutAsync()
        {
            await _tokenManager.ClearAuthDataAsync();
        }

        /// <summary>
        /// Verifica si hay una sesión activa
        /// </summary>
        public async Task<bool> IsAuthenticatedAsync()
        {
            return await _tokenManager.HasActiveSessionAsync();
        }

        /// <summary>
        /// Obtiene los datos del usuario actual
        /// </summary>
        public async Task<AppUser?> GetCurrentUserAsync()
        {
            return await _tokenManager.GetUserDataAsync();
        }

        /// <summary>
        /// Obtiene el refresh token almacenado (para uso en AuthenticationService)
        /// </summary>
        public async Task<string?> GetRefreshTokenAsync()
        {
            return await _tokenManager.GetRefreshTokenAsync();
        }

        #endregion

        #region Métodos Privados

        /// <summary>
        /// Método genérico que maneja todas las peticiones HTTP
        /// </summary>
        private async Task<ApiResponse<T>> SendRequestAsync<T>(
            HttpMethod method,
            string endpoint,
            object? requestData,
            bool requiresAuth,
            bool isRetry = false)
        {
            try
            {
                // Construir URL completa
                var url = $"{_baseUrl}{endpoint}";

                // Crear la petición HTTP
                var request = new HttpRequestMessage(method, url);

                // Agregar body si hay datos
                if (requestData != null && (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch))
                {
                    var jsonContent = JsonSerializer.Serialize(requestData, _jsonOptions);
                    request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                }

                // Agregar token de autenticación si es necesario
                if (requiresAuth)
                {
                    // Verificar si el token está expirado
                    if (await _tokenManager.IsTokenExpiredAsync() && !isRetry)
                    {
                        var refreshed = await RefreshTokenAsync();
                        if (!refreshed)
                        {
                            return new ApiResponse<T>
                            {
                                Success = false,
                                ErrorMessage = "No se pudo refrescar el token. Por favor, inicia sesión nuevamente.",
                                StatusCode = (int)HttpStatusCode.Unauthorized
                            };
                        }
                    }

                    var token = await _tokenManager.GetAccessTokenAsync();

                    if (string.IsNullOrEmpty(token))
                    {
                        return new ApiResponse<T>
                        {
                            Success = false,
                            ErrorMessage = "No hay token de autenticación. Por favor, inicia sesión.",
                            StatusCode = (int)HttpStatusCode.Unauthorized
                        };
                    }

                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                
                var response = new HttpResponseMessage();
                // Enviar la petición
                if (url.Contains("/routines"))
                {
                     response = await _aiHttpClient.SendAsync(request);
                }
                else
                {
                    response = await _httpClient.SendAsync(request);
                }

                // Si recibimos 401 Unauthorized y no es un reintento, refrescar token y reintentar
                if (response.StatusCode == HttpStatusCode.Unauthorized && requiresAuth && !isRetry)
                {
                    var refreshed = await RefreshTokenAsync();

                    if (refreshed)
                    {
                        // Reintentar la petición (marcado como retry)
                        return await SendRequestAsync<T>(method, endpoint, requestData, requiresAuth, isRetry: true);
                    }
                    else
                    {
                        return new ApiResponse<T>
                        {
                            Success = false,
                            ErrorMessage = "Sesión expirada. Por favor, inicia sesión nuevamente.",
                            StatusCode = (int)HttpStatusCode.Unauthorized
                        };
                    }
                }

                // Procesar la respuesta
                return await ProcessResponseAsync<T>(response);
            }
            catch (HttpRequestException ex)
            {
                return new ApiResponse<T>
                {
                    Success = false,
                    ErrorMessage = $"Error de conexión: {ex.Message}",
                    StatusCode = 0
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<T>
                {
                    Success = false,
                    ErrorMessage = $"Error inesperado: {ex.Message}",
                    StatusCode = 0
                };
            }
        }

        /// <summary>
        /// Procesa la respuesta HTTP y deserializa el contenido
        /// </summary>
        private async Task<ApiResponse<T>> ProcessResponseAsync<T>(HttpResponseMessage response)
        {
            var statusCode = (int)response.StatusCode;
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                T? data = default;

                // Intentar deserializar solo si hay contenido
                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    try
                    {
                        data = JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
                    }
                    catch (JsonException ex)
                    {
                        return new ApiResponse<T>
                        {
                            Success = false,
                            ErrorMessage = $"Error al procesar la respuesta: {ex.Message}",
                            StatusCode = statusCode
                        };
                    }
                }

                return new ApiResponse<T>
                {
                    Success = true,
                    Data = data,
                    StatusCode = statusCode
                };
            }
            else
            {
                // Intentar extraer mensaje de error del servidor
                string errorMessage = $"Error del servidor (código {statusCode})";

                if (!string.IsNullOrWhiteSpace(responseContent))
                {
                    try
                    {
                        // Intentar parsear mensaje de error si viene en formato JSON
                        var errorObj = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, _jsonOptions);

                        if (errorObj != null && errorObj.TryGetValue("message", out var message))
                        {
                            errorMessage = message?.ToString() ?? errorMessage;
                        }
                        else if (errorObj != null && errorObj.TryGetValue("error", out var error))
                        {
                            errorMessage = error?.ToString() ?? errorMessage;
                        }
                        else
                        {
                            errorMessage = responseContent;
                        }
                    }
                    catch
                    {
                        // Si no es JSON válido, usar el contenido directamente
                        errorMessage = responseContent;
                    }
                }

                return new ApiResponse<T>
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    StatusCode = statusCode
                };
            }
        }

        /// <summary>
        /// Refresca el token de acceso usando el refresh token
        /// </summary>
        private async Task<bool> RefreshTokenAsync()
        {
            try
            {
                var refreshToken = await _tokenManager.GetRefreshTokenAsync();

                if (string.IsNullOrEmpty(refreshToken))
                {
                    return false;
                }

                // Preparar petición de refresco
                var refreshRequest = new { refreshToken };
                var url = $"{_baseUrl}{_refreshEndpoint}";

                var jsonContent = JsonSerializer.Serialize(refreshRequest, _jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, _jsonOptions);

                    if (authResponse != null)
                    {
                        await _tokenManager.SaveAuthDataAsync(authResponse);
                        return true;
                    }
                }

                // Si falla el refresco, limpiar tokens
                await _tokenManager.ClearAuthDataAsync();
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refrescando token: {ex.Message}");
                await _tokenManager.ClearAuthDataAsync();
                return false;
            }
        }

        #endregion
    }
}