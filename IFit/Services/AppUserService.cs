using IFit.Models;
using IFit.Models.Dtos.AppUser.IFit.Models.Dtos.User;
using IFit.Models.Dtos.IFit.Models.Dtos;
using IFit.Models.Dtos.AppUser;
using System.Diagnostics;

namespace IFit.Services
{
    /// <summary>
    /// Servicio para gestión de usuarios de la aplicación
    /// Utiliza WebService para todas las peticiones HTTP con autenticación automática
    /// </summary>
    public class AppUserService
    {
        private readonly WebService _webService;

        public AppUserService(WebService webService)
        {
            _webService = webService ?? throw new ArgumentNullException(nameof(webService));
        }

        #region Métodos Originales Refactorizados

        /// <summary>
        /// Busca un usuario por su ID
        /// </summary>
        public async Task<AppUserResponseDto?> findUserById(long id)
        {
            if (id <= 0)
            {
                Debug.WriteLine("ID de usuario inválido");
                return null;
            }

            var response = await _webService.GetAsync<AppUserResponseDto>($"/users/{id}");

            if (!response.Success)
            {
                Debug.WriteLine($"Error obteniendo usuario {id}: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Busca un usuario por su email
        /// </summary>
        public async Task<AppUserResponseDto?> findUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                Debug.WriteLine("Email inválido");
                return null;
            }

            var encodedEmail = Uri.EscapeDataString(email);
            var response = await _webService.GetAsync<AppUserResponseDto>($"/users/email/{encodedEmail}");

            if (!response.Success)
            {
                Debug.WriteLine($"Error obteniendo usuario por email: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Asigna un tipo de coach a un usuario
        /// </summary>
        public async Task<AppUserResponseDto?> SetCoachModelType(long? userId, long? coachId)
        {
            if (userId == null || coachId == null || userId <= 0 || coachId <= 0)
            {
                Debug.WriteLine("User ID o Coach ID inválido");
                return null;
            }

            var response = await _webService.PatchAsync<object, AppUserResponseDto>(
                $"/users/{userId}/assign-coach/{coachId}",
                new { }
            );

            if (!response.Success)
            {
                Debug.WriteLine($"Error asignando coach: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Asigna un nivel de experiencia a un usuario
        /// </summary>
        public async Task<AppUserResponseDto?> SetExperienceLevel(long? userId, long? experienceLevel)
        {
            if (userId == null || experienceLevel == null || userId <= 0 || experienceLevel <= 0)
            {
                Debug.WriteLine("User ID o Experience Level ID inválido");
                return null;
            }

            var response = await _webService.PatchAsync<object, AppUserResponseDto>(
                $"/users/{userId}/assign-experience/{experienceLevel}",
                new { }
            );

            if (!response.Success)
            {
                Debug.WriteLine($"Error asignando nivel de experiencia: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Marca el registro de un usuario como completado
        /// </summary>
        public async Task<AppUserResponseDto?> MarkRegistrationComplete(long? userId)
        {
            if (userId == null || userId <= 0)
            {
                Debug.WriteLine("User ID inválido");
                return null;
            }

            var response = await _webService.PatchAsync<object, AppUserResponseDto>(
                $"/users/{userId}/complete-registration",
                new { }
            );

            if (!response.Success)
            {
                Debug.WriteLine($"Error completando registro: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        #endregion

        #region Métodos CRUD Adicionales

        /// <summary>
        /// Obtiene todos los usuarios del sistema
        /// </summary>
        public async Task<List<AppUserResponseDto>?> GetAllUsers()
        {
            var response = await _webService.GetAsync<List<AppUserResponseDto>>("/users");

            if (!response.Success)
            {
                Debug.WriteLine($"Error obteniendo usuarios: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Obtiene usuarios con paginación
        /// </summary>
        public async Task<PagedResponse<AppUserResponseDto>?> GetUsersPaginated(
            int page = 0,
            int size = 20,
            string sortBy = "createdAt",
            string sortDir = "desc")
        {
            var endpoint = $"/users/paginated?page={page}&size={size}&sortBy={sortBy}&sortDir={sortDir}";
            var response = await _webService.GetAsync<PagedResponse<AppUserResponseDto>>(endpoint);

            if (!response.Success)
            {
                Debug.WriteLine($"Error obteniendo usuarios paginados: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Crea un nuevo usuario
        /// </summary>
        public async Task<AppUserResponseDto?> CreateUser(CreateAppUserRequestDto createDto)
        {
            if (createDto == null)
            {
                Debug.WriteLine("DTO de creación es nulo");
                return null;
            }

            var response = await _webService.PostAsync<CreateAppUserRequestDto, AppUserResponseDto>(
                "/users",
                createDto
            );

            if (!response.Success)
            {
                Debug.WriteLine($"Error creando usuario: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Actualiza un usuario existente
        /// </summary>
        public async Task<AppUserResponseDto?> UpdateUser(long id, UpdateAppUserRequestDto updateDto)
        {
            if (id <= 0)
            {
                Debug.WriteLine("ID de usuario inválido");
                return null;
            }

            if (updateDto == null)
            {
                Debug.WriteLine("DTO de actualización es nulo");
                return null;
            }

            var response = await _webService.PutAsync<UpdateAppUserRequestDto, AppUserResponseDto>(
                $"/users/{id}",
                updateDto
            );

            if (!response.Success)
            {
                Debug.WriteLine($"Error actualizando usuario {id}: {response.ErrorMessage}");
                return null;
            }

            return response.Data;
        }

        /// <summary>
        /// Elimina un usuario del sistema
        /// </summary>
        public async Task<bool> DeleteUser(long id)
        {
            if (id <= 0)
            {
                Debug.WriteLine("ID de usuario inválido");
                return false;
            }

            var response = await _webService.DeleteAsync<object>($"/users/{id}");

            if (!response.Success)
            {
                Debug.WriteLine($"Error eliminando usuario {id}: {response.ErrorMessage}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verifica si existe un usuario con el email especificado
        /// </summary>
        public async Task<bool> EmailExists(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                Debug.WriteLine("Email inválido");
                return false;
            }

            var encodedEmail = Uri.EscapeDataString(email);
            var response = await _webService.GetAsync<bool>($"/users/exists/email/{encodedEmail}");

            if (!response.Success)
            {
                Debug.WriteLine($"Error verificando existencia de email: {response.ErrorMessage}");
                return false;
            }

            return response.Data;
        }

        #endregion
    }
}