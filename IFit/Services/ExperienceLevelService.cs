using IFit.Models.Dtos.ExperienceLevel;
using System.Diagnostics;

namespace IFit.Services
{
    /// <summary>
    /// Servicio para gestión de niveles de experiencia de usuarios
    /// Utiliza WebService para todas las peticiones HTTP con autenticación automática
    /// </summary>
    public class ExperienceLevelService
    {
        private readonly WebService _webService;

        public ExperienceLevelService(WebService webService)
        {
            _webService = webService ?? throw new ArgumentNullException(nameof(webService));
        }

        #region Métodos Originales Refactorizados

        /// <summary>
        /// Obtiene todos los niveles de experiencia
        /// </summary>
        public async Task<List<ExperienceLevelDto>?> GetExperienceLevels()
        {
            try
            {
                var response = await _webService.GetAsync<List<ExperienceLevelDto>>("/experience-levels");

                if (!response.Success)
                {
                    Debug.WriteLine($"Error obteniendo experience levels: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetExperienceLevels: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Métodos CRUD Adicionales

        /// <summary>
        /// Obtiene un nivel de experiencia por su ID
        /// </summary>
        public async Task<ExperienceLevelDto?> GetExperienceLevelById(long id)
        {
            try
            {
                if (id <= 0)
                {
                    Debug.WriteLine("Error: ID debe ser mayor que 0");
                    return null;
                }

                var response = await _webService.GetAsync<ExperienceLevelDto>($"/experience-levels/{id}");

                if (!response.Success)
                {
                    Debug.WriteLine($"Error obteniendo experience level {id}: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetExperienceLevelById: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene un nivel de experiencia por su nombre
        /// </summary>
        public async Task<ExperienceLevelDto?> GetExperienceLevelByName(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    Debug.WriteLine("Error: Nombre es null o vacío");
                    return null;
                }

                var encodedName = Uri.EscapeDataString(name);
                var response = await _webService.GetAsync<ExperienceLevelDto>($"/experience-levels/{encodedName}");

                if (!response.Success)
                {
                    Debug.WriteLine($"Error obteniendo experience level por nombre '{name}': {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en GetExperienceLevelByName: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Crea un nuevo nivel de experiencia
        /// Requiere rol de administrador
        /// </summary>
        public async Task<ExperienceLevelDto?> CreateExperienceLevel(CreateExperienceLevelDto createDto)
        {
            try
            {
                if (createDto == null)
                {
                    Debug.WriteLine("Error: DTO de creación es null");
                    return null;
                }

                var response = await _webService.PostAsync<CreateExperienceLevelDto, ExperienceLevelDto>(
                    "/experience-levels",
                    createDto
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error creando experience level: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en CreateExperienceLevel: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Actualiza un nivel de experiencia existente
        /// Requiere rol de administrador
        /// </summary>
        public async Task<ExperienceLevelDto?> UpdateExperienceLevel(long id, UpdateExperienceLevelDto updateDto)
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

                var response = await _webService.PatchAsync<UpdateExperienceLevelDto, ExperienceLevelDto>(
                    $"/experience-levels/{id}",
                    updateDto
                );

                if (!response.Success)
                {
                    Debug.WriteLine($"Error actualizando experience level {id}: {response.ErrorMessage}");
                    return null;
                }

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en UpdateExperienceLevel: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Elimina un nivel de experiencia
        /// Requiere rol de administrador
        /// </summary>
        public async Task<bool> DeleteExperienceLevel(long id)
        {
            try
            {
                if (id <= 0)
                {
                    Debug.WriteLine("Error: ID debe ser mayor que 0");
                    return false;
                }

                var response = await _webService.DeleteAsync<object>($"/experience-levels/{id}");

                if (!response.Success)
                {
                    Debug.WriteLine($"Error eliminando experience level {id}: {response.ErrorMessage}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Excepción en DeleteExperienceLevel: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}