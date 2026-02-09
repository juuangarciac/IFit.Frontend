using System.Diagnostics;
using System.Text;
using IFit.Helper;
using IFit.Models;
using IFit.Models.Dtos.AI;
using IFit.Models.Dtos.Questionnaire;
using SQLite;

namespace IFit.Services
{
    /// <summary>
    /// Servicio para gestión de rutinas generadas por IA y conversaciones con coaches.
    /// 
    /// GESTIÓN DE MEMORIA:
    /// - Cada usuario tiene conversaciones separadas con cada coach
    /// - El memoryId se guarda en SQLite para mantener contexto entre sesiones
    /// - Las conversaciones persisten hasta que el usuario decida iniciar una nueva
    /// 
    /// FLUJO DE GENERACIÓN DE RUTINAS (NUEVO):
    /// 1. Usuario completa un cuestionario → responseId se guarda
    /// 2. Frontend llama a GenerateRoutineAsync(userId, responseId)
    /// 3. Backend iFit construye el prompt automáticamente
    /// 4. Backend llama a Ronnie service con el prompt
    /// 5. Ronnie genera la rutina en formato JSON
    /// 6. Frontend recibe la rutina estructurada
    /// 
    /// ENDPOINTS:
    /// - Generar rutina: POST /api/routines/generate { userId, responseId }
    /// - Chat: POST /chat/{coachName} { memoryId, message }
    /// - Max Memory ID: GET /messages/max-memory-id
    /// </summary>
    public class AIRoutineService
    {
        private readonly WebService _webService;
        private readonly QuestionnaireService _questionnaireService;
        private readonly SQLiteAsyncConnection _database;

        public AIRoutineService(
            WebService webService,
            QuestionnaireService questionnaireService,
            DatabaseService databaseService)
        {
            _webService = webService ?? throw new ArgumentNullException(nameof(webService));
            _questionnaireService = questionnaireService ?? throw new ArgumentNullException(nameof(questionnaireService));
            _database = databaseService?.GetConnection() ?? throw new ArgumentNullException(nameof(databaseService));

            // Inicializar tabla de conversaciones
            _ = InitializeAsync();
        }

        /// <summary>
        /// Inicializa la tabla de conversaciones en SQLite
        /// </summary>
        private async Task InitializeAsync()
        {
            try
            {
                await _database.CreateTableAsync<CoachConversation>();
                Debug.WriteLine("✓ Tabla CoachConversation inicializada correctamente");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Error inicializando tabla CoachConversation: {ex.Message}");
            }
        }

        #region Generación de Rutinas

        /// <summary>
        /// Genera una rutina personalizada llamando al backend iFit.
        /// 
        /// El backend se encarga de:
        /// - Obtener el resumen del cuestionario
        /// - Construir el prompt personalizado
        /// - Llamar al servicio de Ronnie
        /// - Devolver el JSON estructurado
        /// 
        /// NUEVO FLUJO: Frontend solo envía userId + responseId
        /// </summary>
        /// <param name="userId">ID del usuario (String, ej: "user_12345")</param>
        /// <param name="responseId">ID de la respuesta del cuestionario completado</param>
        /// <returns>RoutineResponseDto con la rutina generada o null si hay error</returns>
        public async Task<RoutineDto?> GenerateRoutineAsync(string userId, long responseId)
        {
            try
            {
                Debug.WriteLine("=== Iniciando generación de rutina ===");
                Debug.WriteLine($"UserId: {userId}");
                Debug.WriteLine($"ResponseId: {responseId}");

                // Validar parámetros
                if (string.IsNullOrWhiteSpace(userId))
                {
                    Debug.WriteLine("✗ Error: userId es nulo o vacío");
                    return null;
                }

                if (responseId <= 0)
                {
                    Debug.WriteLine("✗ Error: responseId inválido");
                    return null;
                }

                // Preparar request DTO
                var request = new GenerateRoutineRequestDto
                {
                    UserId = userId,
                    ResponseId = responseId
                };

                Debug.WriteLine($"→ Llamando a POST /api/routines/generate");

                // Llamar al endpoint del backend
                var response = await _webService.PostAsync<GenerateRoutineRequestDto, RoutineDto>(
                    "/routines/generate",
                    request
                );

                // Verificar respuesta
                if (!response.Success)
                {
                    Debug.WriteLine($"✗ Error en la generación: {response.ErrorMessage}");
                    return null;
                }

                if (response.Data == null)
                {
                    Debug.WriteLine("✗ Error: response.Data es null");
                    return null;
                }

                Debug.WriteLine("✓ Rutina generada exitosamente");

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Excepción en GenerateRoutineAsync: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        #endregion

        #region Gestión de Conversaciones

        /// <summary>
        /// Obtiene o crea una conversación activa para un usuario con un coach específico.
        /// Si no existe conversación previa, crea una nueva con un memoryId fresco.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="coachName">Nombre del coach (ronnie, serena, eliud, kael)</param>
        /// <param name="forceNew">Si es true, crea una nueva conversación aunque exista una previa</param>
        /// <returns>Conversación activa o null si hay error</returns>
        public async Task<CoachConversation?> GetOrCreateConversation(
            long userId,
            string coachName,
            bool forceNew = false)
        {
            try
            {
                if (userId <= 0)
                {
                    Debug.WriteLine("✗ Error: userId inválido");
                    return null;
                }

                if (!IsValidCoach(coachName))
                {
                    Debug.WriteLine($"✗ Error: Coach '{coachName}' no es válido");
                    return null;
                }

                var normalizedCoachName = coachName.ToLower().Trim();

                // Si no se fuerza nueva conversación, buscar conversación activa existente
                if (!forceNew)
                {
                    var existingConversation = await _database.Table<CoachConversation>()
                        .Where(c => c.UserId == userId
                                 && c.CoachName == normalizedCoachName
                                 && c.IsActive)
                        .OrderByDescending(c => c.LastUsedAt)
                        .FirstOrDefaultAsync();

                    if (existingConversation != null)
                    {
                        Debug.WriteLine($"✓ Conversación existente encontrada: memoryId={existingConversation.MemoryId}");

                        // Actualizar última fecha de uso
                        existingConversation.LastUsedAt = DateTime.UtcNow;
                        await _database.UpdateAsync(existingConversation);

                        return existingConversation;
                    }
                }

                // Si se fuerza nueva o no existe, archivar conversaciones previas
                if (forceNew)
                {
                    await ArchiveConversations(userId, normalizedCoachName);
                }

                // Crear nueva conversación con nuevo memoryId
                var newMemoryId = await GetNewMemoryIdFromServer();
                if (newMemoryId == null)
                {
                    Debug.WriteLine("✗ No se pudo obtener un nuevo memoryId del servidor");
                    return null;
                }

                var newConversation = new CoachConversation
                {
                    UserId = userId,
                    CoachName = normalizedCoachName,
                    MemoryId = newMemoryId.Value,
                    CreatedAt = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow,
                    IsActive = true,
                    Title = $"Conversación con {GetCoachDescription(normalizedCoachName)}",
                    Notes = "Conversación iniciada automáticamente"
                };

                await _database.InsertAsync(newConversation);
                Debug.WriteLine($"✓ Nueva conversación creada: memoryId={newConversation.MemoryId}");

                return newConversation;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Excepción en GetOrCreateConversation: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Archiva (desactiva) todas las conversaciones activas de un usuario con un coach.
        /// Útil cuando se quiere empezar una conversación desde cero.
        /// </summary>
        private async Task ArchiveConversations(long userId, string coachName)
        {
            try
            {
                var activeConversations = await _database.Table<CoachConversation>()
                    .Where(c => c.UserId == userId
                             && c.CoachName == coachName
                             && c.IsActive)
                    .ToListAsync();

                foreach (var conversation in activeConversations)
                {
                    conversation.IsActive = false;
                    await _database.UpdateAsync(conversation);
                }

                Debug.WriteLine($"✓ Archivadas {activeConversations.Count} conversaciones previas");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Error archivando conversaciones: {ex.Message}");
            }
        }

        /// <summary>
        /// Inicia una nueva conversación con un coach, archivando la conversación previa.
        /// Útil cuando el usuario quiere empezar desde cero sin contexto previo.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="coachName">Nombre del coach</param>
        /// <returns>Nueva conversación o null si hay error</returns>
        public async Task<CoachConversation?> StartNewConversation(long userId, string coachName)
        {
            return await GetOrCreateConversation(userId, coachName, forceNew: true);
        }

        /// <summary>
        /// Obtiene todas las conversaciones de un usuario (activas e inactivas).
        /// </summary>
        public async Task<List<CoachConversation>> GetUserConversations(long userId, bool onlyActive = false)
        {
            try
            {
                var query = _database.Table<CoachConversation>()
                    .Where(c => c.UserId == userId);

                if (onlyActive)
                {
                    query = query.Where(c => c.IsActive);
                }

                var conversations = await query
                    .OrderByDescending(c => c.LastUsedAt)
                    .ToListAsync();

                Debug.WriteLine($"✓ Encontradas {conversations.Count} conversaciones para usuario {userId}");
                return conversations;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Error obteniendo conversaciones: {ex.Message}");
                return new List<CoachConversation>();
            }
        }

        #endregion

        #region Chat con Coaches

        /// <summary>
        /// Envía un mensaje a un coach manteniendo el contexto de conversación.
        /// Si no existe conversación, crea una nueva automáticamente.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="coachName">Nombre del coach</param>
        /// <param name="message">Mensaje para el coach</param>
        /// <param name="startNewConversation">Si es true, inicia conversación nueva</param>
        public async Task<string?> SendMessageToCoach(
            long userId,
            string coachName,
            string message,
            bool startNewConversation = false)
        {
            try
            {
                var conversation = await GetOrCreateConversation(userId, coachName, startNewConversation);
                if (conversation == null)
                {
                    Debug.WriteLine("✗ No se pudo obtener/crear conversación");
                    return null;
                }

                return await ChatWithCoach(coachName, conversation.MemoryId, message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Excepción en SendMessageToCoach: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Chat genérico con cualquier coach por nombre.
        /// Endpoint: POST /chat/{coachName}
        /// </summary>
        /// <param name="coachName">Nombre del coach (ronnie, serena, eliud, kael)</param>
        /// <param name="memoryId">ID de memoria para contexto</param>
        /// <param name="message">Mensaje o prompt para el coach</param>
        /// <returns>Respuesta del coach o null si hay error</returns>
        public async Task<string?> ChatWithCoach(string coachName, int memoryId, string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(coachName))
                {
                    Debug.WriteLine("✗ Error: Nombre de coach vacío");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    Debug.WriteLine("✗ Error: Mensaje vacío");
                    return null;
                }

                // Normalizar el nombre del coach a minúsculas
                var normalizedCoachName = coachName.ToLower().Trim();

                // Validar que sea un coach válido
                if (!IsValidCoach(normalizedCoachName))
                {
                    Debug.WriteLine($"✗ Error: Coach '{coachName}' no es válido");
                    return null;
                }

                // Enviar solicitud
                var endpoint = $"/chat/{normalizedCoachName}";
                var content = new ChatMessageDto { MemoryId = memoryId, Message = message };

                Debug.WriteLine($"→ Llamando a coach '{normalizedCoachName}' con memoryId {memoryId}");

                var response = await _webService.PostAsync<ChatMessageDto, ChatMessageDto>(endpoint, content);

                if (!response.Success)
                {
                    Debug.WriteLine($"✗ Error en chat con {normalizedCoachName}: {response.ErrorMessage}");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(response?.Data?.Message))
                {
                    Debug.WriteLine($"✗ Coach {normalizedCoachName} devolvió respuesta vacía");
                    return null;
                }

                Debug.WriteLine($"✓ Respuesta recibida de {normalizedCoachName} ({response.Data.Message.Length} caracteres)");
                return response.Data.Message;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Excepción en ChatWithCoach ({coachName}): {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return null;
            }
        }

        #endregion

        #region Métodos Auxiliares

        /// <summary>
        /// Obtiene el máximo ID de memoria del servidor AI.
        /// Endpoint: GET /messages/max-memory-id
        /// </summary>
        private async Task<MaxMemoryIdDto?> GetMaxMemoryIdFromServer()
        {
            try
            {
                var response = await _webService.GetAsync<MaxMemoryIdDto>("/messages/max-memory-id");

                if (!response.Success)
                {
                    Debug.WriteLine($"✗ Error obteniendo max memory id: {response.ErrorMessage}");
                    return null;
                }

                if (response.Data == null)
                {
                    Debug.WriteLine("✗ Max memory id response.Data es null");
                    return null;
                }

                Debug.WriteLine($"✓ Max memory ID del servidor: {response.Data.MaxMemoryId}");
                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Excepción en GetMaxMemoryIdFromServer: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene un nuevo ID de memoria para iniciar una conversación.
        /// Calcula el siguiente ID disponible sumando 1 al máximo actual.
        /// </summary>
        /// <returns>Nuevo ID de memoria o null si hay error</returns>
        private async Task<int?> GetNewMemoryIdFromServer()
        {
            try
            {
                var maxMemoryId = await GetMaxMemoryIdFromServer();

                if (maxMemoryId == null)
                {
                    Debug.WriteLine("✗ No se pudo obtener max memory ID");
                    return null;
                }

                var newId = maxMemoryId.MaxMemoryId + 1;
                Debug.WriteLine($"✓ Generado nuevo memory ID del servidor: {newId}");

                return newId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Excepción en GetNewMemoryIdFromServer: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Métodos de Utilidad

        /// <summary>
        /// Obtiene la lista de coaches disponibles
        /// </summary>
        public static List<string> GetAvailableCoaches()
        {
            return new List<string> { "ronnie", "serena", "eliud", "kael" };
        }

        /// <summary>
        /// Obtiene la descripción de un coach específico
        /// </summary>
        public static string GetCoachDescription(string coachName)
        {
            return coachName?.ToLower() switch
            {
                "ronnie" => "Especialista en musculación y entrenamiento de fuerza",
                "serena" => "Experta en yoga, flexibilidad y mindfulness",
                "eliud" => "Entrenador de running y resistencia cardiovascular",
                "kael" => "Coach de HIIT y entrenamiento funcional",
                _ => "Coach no disponible"
            };
        }

        /// <summary>
        /// Valida si un nombre de coach es válido
        /// </summary>
        public static bool IsValidCoach(string coachName)
        {
            if (string.IsNullOrWhiteSpace(coachName))
                return false;

            var validCoaches = GetAvailableCoaches();
            return validCoaches.Contains(coachName.ToLower().Trim());
        }

        #endregion
    }
}