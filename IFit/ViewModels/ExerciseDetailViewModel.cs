using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models.Dtos.Exercise;
using IFit.Services;
using System.Diagnostics;

namespace IFit.ViewModels;

/// <summary>Ítem de instrucción con número de paso para la vista.</summary>
public record InstructionItem(int Number, string Text);

[QueryProperty(nameof(ExerciseId), "ExerciseId")]
public partial class ExerciseDetailViewModel : ObservableObject
{
    #region Properties

    [ObservableProperty]
    public partial string ExerciseId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ExerciseDetailDto? Exercise { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    // Visibilidad de secciones opcionales
    [ObservableProperty] public partial bool HasImages           { get; set; }
    [ObservableProperty] public partial bool HasInstructions     { get; set; }
    [ObservableProperty] public partial bool HasSecondaryMuscles { get; set; }
    [ObservableProperty] public partial bool HasForce            { get; set; }
    [ObservableProperty] public partial bool HasMechanic         { get; set; }

    /// <summary>URLs absolutas de imagen (base URL + ruta relativa del backend).</summary>
    [ObservableProperty]
    public partial List<string> FullImageUrls { get; set; } = new();

    /// <summary>Instrucciones enumeradas (1-based) para mostrar paso a paso.</summary>
    [ObservableProperty]
    public partial List<InstructionItem> NumberedInstructions { get; set; } = new();

    #endregion

    #region Services

    private readonly ExerciseCatalogService _exerciseCatalogService;

    #endregion

    #region Constructor

    public ExerciseDetailViewModel(ExerciseCatalogService exerciseCatalogService)
    {
        _exerciseCatalogService = exerciseCatalogService
            ?? throw new ArgumentNullException(nameof(exerciseCatalogService));
    }

    public ExerciseDetailViewModel() : this(
        App.GetService<ExerciseCatalogService>()
            ?? throw new InvalidOperationException("ExerciseCatalogService no registrado"))
    { }

    #endregion

    #region Partial callbacks

    partial void OnExerciseIdChanged(string value)
    {
        if (long.TryParse(value, out long id) && id > 0)
            _ = LoadExerciseAsync(id);
    }

    partial void OnExerciseChanged(ExerciseDetailDto? value)
    {
        HasImages           = value?.ImageUrls?.Count > 0;
        HasInstructions     = value?.Instructions?.Count > 0;
        HasSecondaryMuscles = value?.SecondaryMuscles?.Count > 0;
        HasForce            = !string.IsNullOrWhiteSpace(value?.Force);
        HasMechanic         = !string.IsNullOrWhiteSpace(value?.Mechanic);

        // Construir URLs absolutas: el backend devuelve rutas relativas (/exercise-images/...)
        FullImageUrls = value?.ImageUrls?
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => AppSettings.ApiGatewayBaseUrl + u)
            .ToList() ?? new();

        // Instrucciones numeradas (1-based) para evitar lógica en XAML
        NumberedInstructions = value?.Instructions?
            .Select((text, idx) => new InstructionItem(idx + 1, text))
            .ToList() ?? new();
    }

    #endregion

    #region Commands

    [RelayCommand]
    public async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    #endregion

    #region Private methods

    private async Task LoadExerciseAsync(long id)
    {
        IsLoading = true;
        StatusMessage = "Cargando ejercicio...";

        try
        {
            Exercise = await _exerciseCatalogService.GetExerciseByIdAsync(id);

            if (Exercise == null)
                StatusMessage = "No se pudo cargar el ejercicio.";
            else
                StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ Error cargando detalle de ejercicio {id}: {ex.Message}");
            StatusMessage = "Error al cargar el ejercicio.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}
