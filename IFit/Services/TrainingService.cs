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
        /// Asignar una rutina ya creada a un usuario.
        /// Esto se hace para que el usuario puede leer la rutina y 
        /// aceptarla o denegarla antes de persistirla en la base de datos.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="routine"></param>
        /// <returns></returns>
        public async Task<RoutineResponseDto?> createRoutineAsync(long userId, RoutineResponseDto routine)
        {
            // El mensaje no se asigna, por que es el generado por la IA, y se asigna en el ViewModel
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

            return response.Data;
        }

        /// <summary>
        /// Obtener las rutinas de un usuario por su ID.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
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
        /// Obtener las rutinas activas de un usuario por su ID.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
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
    }
}
