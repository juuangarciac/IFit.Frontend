using IFit.Models.Dtos.AI;
using IFit.Models.Dtos.Exercise;
using System.Diagnostics;

namespace IFit.Services
{
    /// <summary>
    /// Servicio para el catálogo de ejercicios.
    ///
    /// ENDPOINTS:
    /// - Listar (paginado + filtros): GET /exercises
    /// - Detalle:                     GET /exercises/{id}
    /// </summary>
    public class ExerciseCatalogService
    {
        private readonly WebService _webService;

        public ExerciseCatalogService(WebService webService)
        {
            _webService = webService ?? throw new ArgumentNullException(nameof(webService));
        }

        /// <summary>
        /// Obtiene una página de ejercicios con filtros opcionales.
        /// Todos los filtros son opcionales; si se omiten, el backend devuelve el catálogo completo.
        /// </summary>
        /// <param name="page">Número de página, base 0 (default: 0).</param>
        /// <param name="size">Elementos por página (default: 20).</param>
        /// <param name="level">Filtro de nivel: "principiante" | "intermedio" | "avanzado".</param>
        /// <param name="category">Filtro de categoría: "fuerza" | "cardio" | "estiramiento" | "pliometria".</param>
        /// <param name="equipment">Filtro de equipamiento: "solo_cuerpo" | "barra" | "mancuerna" | "maquina" | ...</param>
        /// <param name="muscle">Búsqueda parcial por músculo, ej: "pecho".</param>
        /// <param name="sortBy">Campo de ordenación (default: "name").</param>
        /// <param name="sortDir">Dirección: "asc" | "desc" (default: "asc").</param>
        public async Task<PagedResponseDto<ExerciseSummaryDto>?> GetExercisesAsync(
            int page = 0,
            int size = 20,
            string? level = null,
            string? category = null,
            string? equipment = null,
            string? muscle = null,
            string sortBy = "name",
            string sortDir = "asc")
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"page={page}",
                    $"size={size}",
                    $"sortBy={Uri.EscapeDataString(sortBy)}",
                    $"sortDir={Uri.EscapeDataString(sortDir)}"
                };

                if (!string.IsNullOrWhiteSpace(level))
                    queryParams.Add($"level={Uri.EscapeDataString(level)}");

                if (!string.IsNullOrWhiteSpace(category))
                    queryParams.Add($"category={Uri.EscapeDataString(category)}");

                if (!string.IsNullOrWhiteSpace(equipment))
                    queryParams.Add($"equipment={Uri.EscapeDataString(equipment)}");

                if (!string.IsNullOrWhiteSpace(muscle))
                    queryParams.Add($"muscle={Uri.EscapeDataString(muscle)}");

                var endpoint = $"/exercises?{string.Join("&", queryParams)}";

                Debug.WriteLine($"→ GET {endpoint}");

                var response = await _webService.GetAsync<PagedResponseDto<ExerciseSummaryDto>>(endpoint);

                if (!response.Success)
                {
                    Debug.WriteLine($"✗ Error al obtener ejercicios: {response.ErrorMessage}");
                    return null;
                }

                Debug.WriteLine($"✓ Ejercicios obtenidos: {response.Data?.Content?.Count ?? 0} " +
                                $"de {response.Data?.TotalElements ?? 0} totales " +
                                $"(pág. {page + 1}/{response.Data?.TotalPages ?? 0})");

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Excepción en GetExercisesAsync: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Obtiene el detalle completo de un ejercicio por su ID.
        /// </summary>
        /// <param name="id">ID del ejercicio.</param>
        public async Task<ExerciseDetailDto?> GetExerciseByIdAsync(long id)
        {
            try
            {
                if (id <= 0)
                {
                    Debug.WriteLine("✗ Error: id de ejercicio inválido");
                    return null;
                }

                Debug.WriteLine($"→ GET /exercises/{id}");

                var response = await _webService.GetAsync<ExerciseDetailDto>($"/exercises/{id}");

                if (!response.Success)
                {
                    Debug.WriteLine($"✗ Error al obtener ejercicio {id}: {response.ErrorMessage}");
                    return null;
                }

                Debug.WriteLine($"✓ Ejercicio obtenido: [{id}] {response.Data?.Name}");

                return response.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"✗ Excepción en GetExerciseByIdAsync: {ex.Message}");
                return null;
            }
        }
    }
}
