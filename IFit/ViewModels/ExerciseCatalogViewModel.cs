using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models.Dtos.Exercise;
using IFit.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace IFit.ViewModels;

public partial class ExerciseCatalogViewModel : ObservableObject
{
    #region Properties

    [ObservableProperty]
    public partial ObservableCollection<ExerciseSummaryDto> Exercises { get; set; } = new();

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

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
    public async Task AppearingAsync()
    {
        // Limpia el overlay de transición si venimos de volver atrás desde el detalle
        IsLoading = false;

        if (_isInitialized) return;
        await LoadExercisesAsync(reset: true);
        _isInitialized = true;
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
        if (!HasMorePages || IsLoading) return;
        await LoadExercisesAsync(reset: false);
    }

    [RelayCommand]
    public async Task OpenDetailAsync(ExerciseSummaryDto exercise)
    {
        if (exercise is null) return;

        // Muestra el overlay antes de navegar para que la transición no parezca un cuelgue
        IsLoading = true;
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

    #endregion

    #region Private methods

    private async Task LoadExercisesAsync(bool reset)
    {
        if (IsLoading) return;

        IsLoading = true;
        StatusMessage = "Cargando ejercicios...";

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
                StatusMessage = string.Empty;
            }
            else
            {
                HasMorePages = false;
                IsEmpty = Exercises.Count == 0;
                StatusMessage = IsEmpty ? "No se encontraron ejercicios." : string.Empty;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ Error cargando ejercicios: {ex.Message}");
            HasMorePages = false;
            IsEmpty = Exercises.Count == 0;
            StatusMessage = "Error al cargar los ejercicios.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion
}
