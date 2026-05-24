using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models.Dtos.Exercise;
using IFit.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace IFit.ViewModels;

[QueryProperty(nameof(InitialSearch), "InitialSearch")]
public partial class ExerciseCatalogViewModel : ObservableObject
{
    #region Properties

    public string InitialSearch
    {
        set { if (!string.IsNullOrWhiteSpace(value)) SearchText = value; }
    }

    [ObservableProperty]
    public partial ObservableCollection<ExerciseSummaryDto> Exercises { get; set; } = new();

    // Overlay de pantalla completa: solo para transiciones de navegación
    [ObservableProperty]
    public partial bool IsNavigating { get; set; }

    // Indicador inline: carga de datos en el CollectionView
    [ObservableProperty]
    public partial bool IsLoadingData { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasMorePages { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    #endregion

    #region Services

    private readonly ExerciseCatalogService _exerciseCatalogService;

    #endregion

    private int _currentPage = 0;
    private const int PageSize = 20;
    private bool _isInitialized = false;

    #region Constructor

    public ExerciseCatalogViewModel(ExerciseCatalogService exerciseCatalogService)
    {
        _exerciseCatalogService = exerciseCatalogService
            ?? throw new ArgumentNullException(nameof(exerciseCatalogService));
    }

    public ExerciseCatalogViewModel() : this(
        App.GetService<ExerciseCatalogService>()
            ?? throw new InvalidOperationException("ExerciseCatalogService no registrado"))
    { }

    #endregion

    #region Commands

    [RelayCommand]
    public Task AppearingAsync()
    {
        // Cierra el overlay de navegación si venimos de volver atrás desde el detalle
        IsNavigating = false;

        if (_isInitialized) return Task.CompletedTask;
        _isInitialized = true;

        // Fire-and-forget: la vista aparece inmediatamente y los datos cargan en segundo plano
        _ = LoadExercisesAsync(reset: true);
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        _isInitialized = false;
        await LoadExercisesAsync(reset: true);
        _isInitialized = true;
    }

    [RelayCommand]
    public async Task LoadMoreAsync()
    {
        if (!HasMorePages || IsLoadingData) return;
        await LoadExercisesAsync(reset: false);
    }

    [RelayCommand]
    public async Task OpenDetailAsync(ExerciseSummaryDto exercise)
    {
        if (exercise is null) return;

        Debug.WriteLine($"→ OpenDetail: Id={exercise.Id}, Name={exercise.Name}");

        IsNavigating = true;
        StatusMessage = "Cargando ejercicio...";

        var parameters = new Dictionary<string, object>
        {
            { "ExerciseId", exercise.Id.ToString() }
        };
        await Shell.Current.GoToAsync("ExerciseDetailView", parameters);
    }

    [RelayCommand]
    public async Task GoToHomeAsync()
    {
        await Shell.Current.GoToAsync("//HomeView");
    }

    [RelayCommand]
    public async Task GoToRoutineBuilderAsync()
    {
        await Shell.Current.GoToAsync("ManualRoutineBuilderView");
    }

    #endregion

    #region Private methods

    private async Task LoadExercisesAsync(bool reset)
    {
        if (IsLoadingData) return;

        IsLoadingData = true;

        if (reset)
        {
            _currentPage = 0;
            Exercises.Clear();
            IsEmpty = false;
        }

        try
        {
            var muscle = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

            var result = await _exerciseCatalogService.GetExercisesAsync(
                page: _currentPage,
                size: PageSize,
                muscle: muscle
            );

            if (result?.Content != null && result.Content.Count > 0)
            {
                foreach (var exercise in result.Content)
                    Exercises.Add(exercise);

                _currentPage++;
                HasMorePages = !result.Last;
                IsEmpty = false;
            }
            else
            {
                HasMorePages = false;
                IsEmpty = Exercises.Count == 0;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ Error cargando ejercicios: {ex.Message}");
            HasMorePages = false;
            IsEmpty = Exercises.Count == 0;
        }
        finally
        {
            IsLoadingData = false;
        }
    }

    #endregion
}
