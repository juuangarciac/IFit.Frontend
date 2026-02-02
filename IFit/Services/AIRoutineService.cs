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
    /// FLUJO DE GENERACIÓN DE RUTINAS:
    /// 1. Usuario completa un cuestionario
    /// 2. Se obtiene el resumen de respuestas del cuestionario
    /// 3. Se construye un prompt personalizado con las respuestas
    /// 4. Se obtiene o crea un memoryId para el coach seleccionado
    /// 5. Se envía el prompt al coach AI
    /// 6. El coach genera la rutina personalizada con contexto previo
    /// 
    /// RUTAS (después de StripPrefix=3 en API Gateway):
    /// - Chat: GET /chat/{coachName}?memoryId={id}&amp;message={msg}
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
                Debug.WriteLine("Tabla CoachConversation inicializada correctamente");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error inicializando tabla CoachConversation: {ex.Message}");
            }
        }

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
                    Debug.WriteLine("Error: userId inválido");
                    return null;
                }

                if (!IsValidCoach(coachName))
                {
                    Debug.WriteLine($"Error: Coach '{coachName}' no es válido");
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
                        Debug.WriteLine($"Conversación existente encontrada: memoryId={existingConversation.MemoryId}");

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
                    Debug.WriteLine("No se pudo obtener un nuevo memoryId del servidor");
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
                Debug.WriteLine($"Nueva conversación creada: memoryId={newConversation.MemoryId}");

                return newConversation;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetOrCreateConversation: {ex.Message}");
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

                Debug.WriteLine($"Archivadas {activeConversations.Count} conversaciones previas");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error archivando conversaciones: {ex.Message}");
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

                return await query.OrderByDescending(c => c.LastUsedAt).ToListAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error obteniendo conversaciones: {ex.Message}");
                return new List<CoachConversation>();
            }
        }

        /// <summary>
        /// Actualiza el título de una conversación.
        /// </summary>
        public async Task<bool> UpdateConversationTitle(int conversationId, string newTitle)
        {
            try
            {
                var conversation = await _database.Table<CoachConversation>()
                    .Where(c => c.Id == conversationId)
                    .FirstOrDefaultAsync();

                if (conversation == null)
                {
                    Debug.WriteLine($"Conversación {conversationId} no encontrada");
                    return false;
                }

                conversation.Title = newTitle;
                conversation.LastUsedAt = DateTime.UtcNow;
                await _database.UpdateAsync(conversation);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error actualizando título: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Generación de Rutinas

        /// <summary>
        /// Genera una rutina completa a partir de un cuestionario completado.
        /// Mantiene el contexto de conversaciones previas con el coach.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="responseId">ID de la sesión de cuestionario completada</param>
        /// <param name="coachName">Nombre del coach: ronnie, serena, eliud, kael</param>
        /// <param name="startNewConversation">Si es true, inicia conversación nueva sin contexto previo</param>
        /// <returns>Rutina generada por el coach o null si hay error</returns>
        public async Task<string?> GenerateRoutineFromQuestionnaire(
            long userId,
            long responseId,
            string coachName,
            bool startNewConversation = false)
        {
            try
            {
                // 1. Validar parámetros de entrada
                if (userId <= 0)
                {
                    Debug.WriteLine("Error: userId inválido");
                    return null;
                }

                if (responseId <= 0)
                {
                    Debug.WriteLine("Error: responseId inválido");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(coachName))
                {
                    Debug.WriteLine("Error: Nombre de coach vacío");
                    return null;
                }

                // 2. Obtener el resumen de respuestas del cuestionario
                var summary = await _questionnaireService.GetResponseSummary(responseId);

                if (summary == null)
                {
                    Debug.WriteLine($"No se encontró resumen para responseId {responseId}");
                    await ErrorHandler.HandleErrorAsync(
                        "Error",
                        "No se pudo obtener la información del cuestionario."
                    );
                    return null;
                }

                // 3. Verificar que el cuestionario esté completado
                if (!summary.IsCompleted)
                {
                    Debug.WriteLine($"El cuestionario {responseId} no está completado");
                    await ErrorHandler.HandleErrorAsync(
                        "Cuestionario Incompleto",
                        "Debes completar todas las preguntas antes de generar tu rutina."
                    );
                    return null;
                }

                // 4. Verificar que haya respuestas
                if (summary.Answers == null || summary.Answers.Count == 0)
                {
                    Debug.WriteLine($"No hay respuestas en el cuestionario {responseId}");
                    await ErrorHandler.HandleErrorAsync(
                        "Error",
                        "El cuestionario no tiene respuestas registradas."
                    );
                    return null;
                }

                // 5. Construir el prompt para el coach
                var prompt = BuildRoutinePrompt(summary);

                if (string.IsNullOrWhiteSpace(prompt))
                {
                    Debug.WriteLine("No se pudo construir el prompt");
                    return null;
                }

                Debug.WriteLine($"Prompt generado con {summary.Answers.Count} respuestas");

                // 6. Obtener o crear conversación con el coach
                var conversation = await GetOrCreateConversation(userId, coachName, startNewConversation);
                if (conversation == null)
                {
                    Debug.WriteLine("No se pudo obtener/crear conversación");
                    await ErrorHandler.HandleErrorAsync(
                        "Error de Conexión",
                        "No pudimos conectar con el asistente IA."
                    );
                    return null;
                }

                Debug.WriteLine($"Usando memoryId: {conversation.MemoryId}");

                // 7. Enviar el prompt al coach para generar la rutina
                var routine = await ChatWithCoach(coachName, conversation.MemoryId, prompt);

                if (string.IsNullOrWhiteSpace(routine))
                {
                    Debug.WriteLine("El coach no generó una rutina válida");
                    await ErrorHandler.HandleErrorAsync(
                        "Error",
                        "No se pudo generar la rutina. Por favor, inténtalo de nuevo."
                    );
                    return null;
                }

                // 8. Actualizar título de conversación con info del cuestionario
                if (conversation.Title?.Contains("automáticamente") == true)
                {
                    await UpdateConversationTitle(
                        conversation.Id,
                        $"Rutina basada en {summary.QuestionnaireName}"
                    );
                }

                Debug.WriteLine("Rutina generada exitosamente con contexto persistente");
                return routine;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GenerateRoutineFromQuestionnaire: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                await ErrorHandler.HandleErrorAsync(
                    "Error Inesperado",
                    "Ocurrió un error al generar tu rutina."
                );
                return null;
            }
        }

        /// <summary>
        /// Construye el prompt para el coach AI basado en las respuestas del cuestionario.
        /// Estructura el prompt de forma clara y organizada para mejorar la calidad de la respuesta.
        /// </summary>
        /// <param name="summary">Resumen del cuestionario con todas las respuestas</param>
        /// <returns>Prompt estructurado para el coach</returns>
        private string BuildRoutinePrompt(QuestionnaireResponseSummaryDTO summary)
        {
            var promptBuilder = new StringBuilder();

            // Introducción del prompt
            promptBuilder.AppendLine("Por favor, genera una rutina de entrenamiento personalizada basada en la siguiente información del usuario:");
            promptBuilder.AppendLine();

            // Información del usuario del cuestionario
            promptBuilder.AppendLine("=== PERFIL DEL USUARIO ===");
            promptBuilder.AppendLine($"Usuario: {summary.UserName}");
            promptBuilder.AppendLine($"Cuestionario: {summary.QuestionnaireName}");
            promptBuilder.AppendLine();

            // Respuestas del cuestionario
            promptBuilder.AppendLine("=== RESPUESTAS AL CUESTIONARIO ===");
            foreach (var answer in summary.Answers)
            {
                promptBuilder.AppendLine($"• {answer.QuestionText}");
                promptBuilder.AppendLine($"  Respuesta: {answer.SelectedOptionText}");

                // Incluir texto adicional si existe
                if (!string.IsNullOrWhiteSpace(answer.AdditionalText))
                {
                    promptBuilder.AppendLine($"  Detalles: {answer.AdditionalText}");
                }

                promptBuilder.AppendLine();
            }

            // Instrucciones específicas para el coach
            promptBuilder.AppendLine("=== INSTRUCCIONES ===");
            promptBuilder.AppendLine("Con base en esta información, por favor genera una rutina que incluya:");
            promptBuilder.AppendLine("1. Planificación semanal completa (días de entrenamiento y descanso)");
            promptBuilder.AppendLine("2. Ejercicios específicos para cada día");
            promptBuilder.AppendLine("3. Series, repeticiones y/o duración de cada ejercicio");
            promptBuilder.AppendLine("4. Consideraciones especiales (calentamiento, enfriamiento, progresión)");
            promptBuilder.AppendLine("5. Recomendaciones adicionales según el perfil del usuario");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Adapta la rutina al nivel de experiencia, objetivos y disponibilidad del usuario.");

            return promptBuilder.ToString();
        }

        /// <summary>
        /// Genera una rutina utilizando el coach de IA con un prompt personalizado.
        /// Mantiene el contexto de la conversación activa con el coach.
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="coachName">Nombre del coach: ronnie, serena, eliud, kael</param>
        /// <param name="prompt">Prompt personalizado para el coach</param>
        /// <param name="startNewConversation">Si es true, inicia conversación nueva</param>
        /// <returns>Respuesta del coach o null si hay error</returns>
        public async Task<string?> GenerateRoutineFromCustomPrompt(
            long userId,
            string coachName,
            string prompt,
            bool startNewConversation = false)
        {
            try
            {
                if (userId <= 0)
                {
                    Debug.WriteLine("Error: userId inválido");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(coachName))
                {
                    Debug.WriteLine("Error: Nombre de coach vacío");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(prompt))
                {
                    Debug.WriteLine("Error: Prompt vacío");
                    return null;
                }

                var conversation = await GetOrCreateConversation(userId, coachName, startNewConversation);
                if (conversation == null)
                {
                    await ErrorHandler.HandleErrorAsync(
                        "Error de Conexión",
                        "No pudimos conectar con el asistente IA."
                    );
                    return null;
                }

                return await ChatWithCoach(coachName, conversation.MemoryId, prompt);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GenerateRoutineFromCustomPrompt: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Chat con Coaches

        /// <summary>
        /// Envía un mensaje a un coach manteniendo el contexto de conversación.
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
                    Debug.WriteLine("No se pudo obtener/crear conversación");
                    return null;
                }

                return await ChatWithCoach(coachName, conversation.MemoryId, message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en SendMessageToCoach: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Chat genérico con cualquier coach por nombre.
        /// Endpoint: GET /chat/{coachName}?memoryId={id}&amp;message={msg}
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
                    Debug.WriteLine("Error: Nombre de coach vacío");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(message))
                {
                    Debug.WriteLine("Error: Mensaje vacío");
                    return null;
                }

                // Normalizar el nombre del coach a minúsculas
                var normalizedCoachName = coachName.ToLower().Trim();

                // Validar que sea un coach válido
                if (!IsValidCoach(normalizedCoachName))
                {
                    Debug.WriteLine($"Error: Coach '{coachName}' no es válido");
                    return null;
                }

                // Codificar el mensaje para URL
                var encodedMessage = Uri.EscapeDataString(message);
                var endpoint = $"/chat/{normalizedCoachName}?memoryId={memoryId}&message={encodedMessage}";

                Debug.WriteLine($"Llamando a coach '{normalizedCoachName}' con memoryId {memoryId}");

                var response = await _webService.GetAsync<string>(endpoint);

                if (!response.Success)
                {
                    Debug.WriteLine($"Error en chat con {normalizedCoachName}: {response.ErrorMessage}");
                    return null;
                }

                if (string.IsNullOrWhiteSpace(response.Data))
                {
                    Debug.WriteLine($"Coach {normalizedCoachName} devolvió respuesta vacía");
                    return null;
                }

                Debug.WriteLine($"Respuesta recibida de {normalizedCoachName} ({response.Data.Length} caracteres)");
                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en ChatWithCoach ({coachName}): {ex.Message}");
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
                    Debug.WriteLine($"Error obteniendo max memory id: {response.ErrorMessage}");
                    return null;
                }

                if (response.Data == null)
                {
                    Debug.WriteLine("Max memory id response.Data es null");
                    return null;
                }

                Debug.WriteLine($"Max memory ID del servidor: {response.Data.MaxMemoryId}");
                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetMaxMemoryIdFromServer: {ex.Message}");
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
                    Debug.WriteLine("No se pudo obtener max memory ID");
                    return null;
                }

                var newId = maxMemoryId.MaxMemoryId + 1;
                Debug.WriteLine($"Generado nuevo memory ID del servidor: {newId}");

                return newId;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetNewMemoryIdFromServer: {ex.Message}");
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