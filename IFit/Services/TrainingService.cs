using IFit.Models.Dtos.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.Services
{
    public class TrainingService
    {
        #region Services
        private readonly WebService _webService;
        #endregion

        public TrainingService(WebService webService)
        {
            _webService = webService ?? throw new ArgumentNullException(nameof(webService));
        }

        /// <summary>
        /// Persiste una rutina generada por la IA en la base de datos.
        /// Se llama tras aceptar la rutina previamente mostrada al usuario.
        /// POST /routines
        /// </summary>
        public async Task<RoutineResponseDto?> createRoutineAsync(long userId, RoutineResponseDto routine)
        {
            CreateRoutineRequestDto request = new CreateRoutineRequestDto
            {
                UserId = userId,
                Description = routine.Description,
                TrainingDays = routine.TrainingDays,
                Days = routine.Days
            };

            var response = await _webService.PostAsync<CreateRoutineRequestDto, RoutineResponseDto>("/routines", request);

            if (!response.Success)
            {
                Debug.WriteLine($"Error creando rutina para el usuario {userId}: {response.ErrorMessage}");
                return null;
            }
            Preferences.Set("CurrentRoutineId", response.Data?.Id ?? 0L); // Guardar el ID de la rutina creada para futuras referencias

            return response.Data;
        }

        /// <summary>
        /// Solicita al backend la generación de una rutina personalizada con IA (Ronnie).
        /// POST /routines/generate
        /// </summary>
        public async Task<RoutineResponseDto?> generateRoutineAsync(string userId, long responseId)
        {
            GenerateRoutineRequestDto request = new GenerateRoutineRequestDto
            {
                UserId = userId,
                ResponseId = responseId
            };

            var response = await _webService.PostAsync<GenerateRoutineRequestDto, RoutineResponseDto>("/routines/generate", request);

            if (!response.Success)
            {
                Debug.WriteLine($"Error generando rutina para el usuario {userId}: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        // ─────────────────────────────────────────────────────────────────────
        // READ
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Obtiene todas las rutinas del sistema (sin paginación).
        /// GET /routines
        /// </summary>
        public async Task<List<RoutineResponseDto>?> getAllRoutinesAsync()
        {
            var response = await _webService.GetAsync<List<RoutineResponseDto>>("/routines");

            if (!response.Success)
            {
                Debug.WriteLine($"Error obteniendo todas las rutinas: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Obtiene una rutina por su ID.
        /// GET /routines/{id}
        /// </summary>
        public async Task<RoutineResponseDto?> getRoutineByIdAsync(long id)
        {
            var response = await _webService.GetAsync<RoutineResponseDto>($"/routines/{id}");

            if (!response.Success)
            {
                Debug.WriteLine($"Error obteniendo rutina con id {id}: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Obtiene las rutinas de un usuario por su ID.
        /// GET /routines/user/{userId}
        /// </summary>
        public async Task<List<RoutineResponseDto>?> getRoutinesByUserIdAsync(long userId)
        {
            var response = await _webService.GetAsync<List<RoutineResponseDto>>($"/routines/user/{userId}");

            if (!response.Success)
            {
                Debug.WriteLine($"Error obteniendo rutinas del usuario {userId}: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Obtiene las rutinas activas de un usuario por su ID.
        /// GET /routines/user/{userId}/active
        /// </summary>
        public async Task<List<RoutineResponseDto>?> getActivesRoutinesByUserIdAsync(long userId)
        {
            var response = await _webService.GetAsync<List<RoutineResponseDto>>($"/routines/user/{userId}/active");

            if (!response.Success)
            {
                Debug.WriteLine($"Error obteniendo rutinas activas del usuario {userId}: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Obtiene las rutinas de un usuario con paginación y ordenamiento.
        /// GET /routines/user/{userId}/paginated?page=0&size=10&sortBy=createdAt&sortDir=desc
        /// </summary>
        public async Task<PagedResponseDto<RoutineResponseDto>?> getRoutinesByUserIdPaginatedAsync(
            long userId, int page = 0, int size = 10, string sortBy = "createdAt", string sortDir = "desc")
        {
            string url = $"/routines/user/{userId}/paginated?page={page}&size={size}&sortBy={sortBy}&sortDir={sortDir}";
            var response = await _webService.GetAsync<PagedResponseDto<RoutineResponseDto>>(url);

            if (!response.Success)
            {
                Debug.WriteLine($"Error obteniendo rutinas paginadas del usuario {userId}: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Cuenta las rutinas activas de un usuario.
        /// GET /routines/user/{userId}/count-active
        /// </summary>
        public async Task<long?> countActiveRoutinesByUserIdAsync(long userId)
        {
            var response = await _webService.GetAsync<long>($"/routines/user/{userId}/count-active");

            if (!response.Success)
            {
                Debug.WriteLine($"Error contando rutinas activas del usuario {userId}: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Obtiene el detalle de un día específico de una rutina.
        /// GET /routines/{routineId}/day/{day}
        /// </summary>
        public async Task<TrainingDayDto?> getRoutineDayAsync(long routineId, int day)
        {
            var response = await _webService.GetAsync<TrainingDayDto>($"/routines/{routineId}/day/{day}");

            if (!response.Success)
            {
                Debug.WriteLine($"Error obteniendo día {day} de la rutina {routineId}: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Actualiza una rutina existente (actualización parcial).
        /// PUT /routines/{id}
        /// </summary>
        public async Task<RoutineResponseDto?> updateRoutineAsync(long id, UpdateRoutineRequestDto updateDto)
        {
            var response = await _webService.PutAsync<UpdateRoutineRequestDto, RoutineResponseDto>($"/routines/{id}", updateDto);

            if (!response.Success)
            {
                Debug.WriteLine($"Error actualizando rutina {id}: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Activa o desactiva una rutina.
        /// PATCH /routines/{id}/toggle-active?isActive=true
        /// </summary>
        public async Task<RoutineResponseDto?> toggleRoutineActiveAsync(long id, bool isActive)
        {
            string url = $"/routines/{id}/toggle-active?isActive={isActive.ToString().ToLower()}";
            var response = await _webService.PatchAsync<object, RoutineResponseDto>(url, null);

            if (!response.Success)
            {
                Debug.WriteLine($"Error cambiando estado de la rutina {id}: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Marca un día de la rutina como completado y avanza al siguiente día.
        /// POST /routines/{routineId}/day/{day}/complete
        /// </summary>
        public async Task<RoutineResponseDto?> setRoutineDayAsCompletedAsync(long routineId, int day)
        {
            var response = await _webService.PostAsync<object, RoutineResponseDto>(
                $"/routines/{routineId}/day/{day}/complete", null);

            if (!response.Success)
            {
                Debug.WriteLine($"Error marcando día {day} de la rutina {routineId} como completado: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Marca una rutina como completada.
        /// POST /routines/{id}/complete
        /// </summary>
        public async Task<RoutineResponseDto?> completeRoutineAsync(long id)
        {
            var response = await _webService.PostAsync<object, RoutineResponseDto>(
                $"/routines/{id}/complete", null);

            if (!response.Success)
            {
                Debug.WriteLine($"Error completando rutina {id}: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }
    }
}