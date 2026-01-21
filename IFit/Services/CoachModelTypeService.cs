using IFit.Models;
using IFit.Models.Dtos.Coach;
using System.Diagnostics;

namespace IFit.Services
{
    /// <summary>
    /// Servicio para gestión de tipos de modelo de coach de IA
    /// Utiliza WebService para todas las peticiones HTTP con autenticación automática
    /// </summary>
    public class CoachModelTypeService
    {
        private readonly WebService _webService;

        public CoachModelTypeService(WebService webService)
        {
            _webService = webService ?? throw new ArgumentNullException(nameof(webService));
        }

        public CoachModelTypeService()
        {
        }

        #region Métodos Originales Refactorizados

        /// <summary>
        /// Obtiene todos los tipos de modelo de coach habilitados
        /// </summary>
        public async Task<List<CoachModelTypeResponseDto>?> GetCoachModelTypes()
        {
            try
            {
                var response = await _webService.GetAsync<List<CoachModelTypeResponseDto>>("/coach-models");

                if (!response.Success)
                {
                    Debug.WriteLine($"Error obteniendo coach models: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetCoachModelTypes: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene un tipo de modelo de coach por su ID
        /// </summary>
        public async Task<CoachModelTypeResponseDto?> GetCoachModelTypeById(string? id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    Debug.WriteLine("Error: ID es null o vacío");
                    return null;
                }

                if (!long.TryParse(id, out long coachId))
                {
                    Debug.WriteLine($"Error: ID '{id}' no es un número válido");
                    return null;
                }

                var response = await _webService.GetAsync<CoachModelTypeResponseDto>($"/coach-models/{coachId}");

                if (!response.Success)
                {
                    Debug.WriteLine($"Error obteniendo coach model {coachId}: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetCoachModelTypeById: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Métodos CRUD Adicionales

        /// <summary>
        /// Obtiene todos los tipos de modelo de coach (habilitados y deshabilitados)
        /// Requiere rol de administrador
        /// </summary>
        public async Task<List<CoachModelTypeResponseDto>?> GetAllCoachModelTypes()
        {
            try
            {
                var response = await _webService.GetAsync<List<CoachModelTypeResponseDto>>("/coach-models/all");

                if (!response.Success)
                {
                    Debug.WriteLine($"Error obteniendo todos los coach models: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetAllCoachModelTypes: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene un tipo de modelo de coach por su ID (versión con long)
        /// </summary>
        public async Task<CoachModelTypeResponseDto?> GetCoachModelTypeById(long id)
        {
            try
            {
                if (id <= 0)
                {
                    Debug.WriteLine("Error: ID debe ser mayor que 0");
                    return null;
                }

                var response = await _webService.GetAsync<CoachModelTypeResponseDto>($"/coach-models/{id}");

                if (!response.Success)
                {
                    Debug.WriteLine($"Error obteniendo coach model {id}: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetCoachModelTypeById: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Busca un tipo de modelo de coach por su nombre
        /// </summary>
        public async Task<CoachModelTypeResponseDto?> GetCoachModelTypeByName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    Debug.WriteLine("Error: Nombre es null o vacío");
                    return null;
                }

                var encodedName = Uri.EscapeDataString(name);
                var response = await _webService.GetAsync<CoachModelTypeResponseDto>($"/coach-models/name/{encodedName}");

                if (!response.Success)
                {
                    Debug.WriteLine($"Error obteniendo coach model por nombre '{name}': {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetCoachModelTypeByName: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Crea un nuevo tipo de modelo de coach
        /// Requiere rol de administrador
        /// </summary>
        public async Task<CoachModelTypeResponseDto?> CreateCoachModelType(CreateCoachModelTypeRequestDto createDto)
        {
            try
            {
                if (createDto == null)
                {
                    Debug.WriteLine("Error: DTO de creación es null");
                    return null;
                }

                var response = await _webService.PostAsync<CreateCoachModelTypeRequestDto, CoachModelTypeResponseDto>(
                    "/coach-models",
                    createDto
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error creando coach model: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en CreateCoachModelType: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Actualiza un tipo de modelo de coach existente
        /// Requiere rol de administrador
        /// </summary>
        public async Task<CoachModelTypeResponseDto?> UpdateCoachModelType(long id, UpdateCoachModelTypeRequestDto updateDto)
        {
            try
            {
                if (id <= 0)
                {
                    Debug.WriteLine("Error: ID debe ser mayor que 0");
                    return null;
                }

                if (updateDto == null)
                {
                    Debug.WriteLine("Error: DTO de actualización es null");
                    return null;
                }

                var response = await _webService.PutAsync<UpdateCoachModelTypeRequestDto, CoachModelTypeResponseDto>(
                    $"/coach-models/{id}",
                    updateDto
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error actualizando coach model {id}: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en UpdateCoachModelType: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deshabilita (soft delete) un tipo de modelo de coach
        /// Requiere rol de administrador
        /// </summary>
        public async Task<bool> DeleteCoachModelType(long id)
        {
            try
            {
                if (id <= 0)
                {
                    Debug.WriteLine("Error: ID debe ser mayor que 0");
                    return false;
                }

                var response = await _webService.DeleteAsync<object>($"/coach-models/{id}");

                if (!response.Success)
                {
                    Debug.WriteLine($"Error deshabilitando coach model {id}: {response.ErrorMessage}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en DeleteCoachModelType: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Habilita un tipo de modelo de coach previamente deshabilitado
        /// Requiere rol de administrador
        /// </summary>
        public async Task<CoachModelTypeResponseDto?> EnableCoachModelType(long id)
        {
            try
            {
                if (id <= 0)
                {
                    Debug.WriteLine("Error: ID debe ser mayor que 0");
                    return null;
                }

                var response = await _webService.PutAsync<object, CoachModelTypeResponseDto>(
                    $"/coach-models/{id}/enable",
                    new { }
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error habilitando coach model {id}: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en EnableCoachModelType: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}