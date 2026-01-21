using IFit.Models.Dtos.Questionnaire;
using System.Diagnostics;

namespace IFit.Services
{
    /// <summary>
    /// Servicio para gestión de cuestionarios y sesiones de respuesta
    /// Utiliza WebService para todas las peticiones HTTP con autenticación automática
    /// </summary>
    public class QuestionnaireService
    {
        private readonly WebService _webService;

        public QuestionnaireService(WebService webService)
        {
            _webService = webService ?? throw new ArgumentNullException(nameof(webService));
        }

        #region CRUD de Cuestionarios (Plantillas)

        /// <summary>
        /// Obtiene todos los cuestionarios habilitados en formato resumido
        /// </summary>
        public async Task<List<QuestionnaireSummaryDto>?> GetAllQuestionnaires()
        {
            try
            {
                var response = await _webService.GetAsync<List<QuestionnaireSummaryDto>>("/questionnaires");

                if (!response.Success)
                {
                    Debug.WriteLine($"Error obteniendo cuestionarios: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetAllQuestionnaires: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene un cuestionario completo por su ID
        /// </summary>
        public async Task<QuestionnaireDTO?> GetQuestionnaireById(long id)
        {
            try
            {
                if (id <= 0)
                {
                    Debug.WriteLine("Error: ID de cuestionario inválido");
                    return null;
                }

                var response = await _webService.GetAsync<QuestionnaireDTO>($"/questionnaires/{id}");

                if (!response.Success)
                {
                    Debug.WriteLine($"Error obteniendo cuestionario {id}: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetQuestionnaireById: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene un cuestionario con su primera pregunta incluida
        /// Endpoint optimizado que reduce llamadas a la API
        /// </summary>
        public async Task<QuestionnaireWithFirstQuestionDto?> GetQuestionnaireWithFirstQuestion(long id)
        {
            try
            {
                if (id <= 0)
                {
                    Debug.WriteLine("Error: ID de cuestionario inválido");
                    return null;
                }

                var response = await _webService.GetAsync<QuestionnaireWithFirstQuestionDto>(
                    $"/questionnaires/{id}/with-first-question"
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error obteniendo cuestionario con primera pregunta {id}: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetQuestionnaireWithFirstQuestion: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Crea un nuevo cuestionario (solo administradores)
        /// </summary>
        public async Task<QuestionnaireDTO?> CreateQuestionnaire(CreateQuestionnaireRequestDto createDto)
        {
            try
            {
                if (createDto == null)
                {
                    Debug.WriteLine("Error: DTO de creación es null");
                    return null;
                }

                var response = await _webService.PostAsync<CreateQuestionnaireRequestDto, QuestionnaireDTO>(
                    "/questionnaires",
                    createDto
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error creando cuestionario: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en CreateQuestionnaire: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Actualiza un cuestionario existente (solo administradores)
        /// </summary>
        public async Task<QuestionnaireDTO?> UpdateQuestionnaire(long id, UpdateQuestionnaireRequestDto updateDto)
        {
            try
            {
                if (id <= 0)
                {
                    Debug.WriteLine("Error: ID de cuestionario inválido");
                    return null;
                }

                if (updateDto == null)
                {
                    Debug.WriteLine("Error: DTO de actualización es null");
                    return null;
                }

                var response = await _webService.PutAsync<UpdateQuestionnaireRequestDto, QuestionnaireDTO>(
                    $"/questionnaires/{id}",
                    updateDto
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error actualizando cuestionario {id}: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en UpdateQuestionnaire: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Elimina un cuestionario (solo administradores)
        /// </summary>
        public async Task<bool> DeleteQuestionnaire(long id)
        {
            try
            {
                if (id <= 0)
                {
                    Debug.WriteLine("Error: ID de cuestionario inválido");
                    return false;
                }

                var response = await _webService.DeleteAsync<object>($"/questionnaires/{id}");

                if (!response.Success)
                {
                    Debug.WriteLine($"Error eliminando cuestionario {id}: {response.ErrorMessage}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en DeleteQuestionnaire: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Sesiones de Usuario (Respuestas)

        /// <summary>
        /// Inicia una nueva sesión de cuestionario para un usuario
        /// Retorna el ID de sesión (responseId) y la primera pregunta
        /// </summary>
        public async Task<QuestionnaireResponseDTO?> StartQuestionnaire(long userId, long questionnaireId)
        {
            try
            {
                if (userId <= 0 || questionnaireId <= 0)
                {
                    Debug.WriteLine("Error: userId o questionnaireId inválidos");
                    return null;
                }

                var response = await _webService.PostAsync<object, QuestionnaireResponseDTO>(
                    $"/questionnaires/{userId}/start/{questionnaireId}",
                    new { } // Body vacío
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error iniciando cuestionario: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en StartQuestionnaire: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Responde a una pregunta del cuestionario
        /// Retorna la siguiente pregunta automáticamente o marca como completado
        /// </summary>
        public async Task<QuestionnaireResponseDTO?> AnswerQuestion(
            long responseId,
            AnswerRequestDTO answerRequest)
        {
            try
            {
                if (responseId <= 0)
                {
                    Debug.WriteLine("Error: responseId inválido");
                    return null;
                }

                if (answerRequest == null)
                {
                    Debug.WriteLine("Error: answerRequest es null");
                    return null;
                }

                var response = await _webService.PostAsync<AnswerRequestDTO, QuestionnaireResponseDTO>(
                    $"/questionnaires/responses/{responseId}/answer",
                    answerRequest
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error respondiendo pregunta: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en AnswerQuestion: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene el resumen completo de una sesión de cuestionario
        /// Incluye todas las respuestas del usuario
        /// </summary>
        public async Task<QuestionnaireResponseSummaryDTO?> GetResponseSummary(long responseId)
        {
            try
            {
                if (responseId <= 0)
                {
                    Debug.WriteLine("Error: responseId inválido");
                    return null;
                }

                var response = await _webService.GetAsync<QuestionnaireResponseSummaryDTO>(
                    $"/questionnaires/responses/{responseId}/summary"
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error obteniendo resumen de sesión {responseId}: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetResponseSummary: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene todas las sesiones de cuestionarios del usuario autenticado
        /// Incluye sesiones completadas y activas
        /// </summary>
        public async Task<List<QuestionnaireResponse>?> GetMyResponses()
        {
            try
            {
                var response = await _webService.GetAsync<List<QuestionnaireResponse>>(
                    "/questionnaires/responses/my-responses"
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error obteniendo mis respuestas: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetMyResponses: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene las sesiones completadas del usuario autenticado
        /// </summary>
        public async Task<List<QuestionnaireResponse>?> GetMyCompletedResponses()
        {
            try
            {
                var response = await _webService.GetAsync<List<QuestionnaireResponse>>(
                    "/questionnaires/responses/my-completed-responses"
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error obteniendo respuestas completadas: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetMyCompletedResponses: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene las sesiones activas (no completadas) del usuario autenticado
        /// </summary>
        public async Task<List<QuestionnaireResponse>?> GetMyActiveResponses()
        {
            try
            {
                var response = await _webService.GetAsync<List<QuestionnaireResponse>>(
                    "/questionnaires/responses/my-active-responses"
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error obteniendo respuestas activas: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetMyActiveResponses: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}