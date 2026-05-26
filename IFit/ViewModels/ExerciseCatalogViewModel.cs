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
    private readonly DatabaseService _databaseService;
    private CancellationTokenSource? _liveCts;

    #endregion

    private int _currentPage = 0;
    private const int PageSize = 20;
    private bool _isInitialized = false;

    #region Constructor

    public ExerciseCatalogViewModel(
        ExerciseCatalogService exerciseCatalogService,
        DatabaseService databaseService)
    {
        _exerciseCatalogService = exerciseCatalogService
            ?? throw new ArgumentNullException(nameof(exerciseCatalogService));
        _databaseService = databaseService
            ?? throw new ArgumentNullException(nameof(databaseService));
    }

    public ExerciseCatalogViewModel() : this(
        App.GetService<ExerciseCatalogService>()
            ?? throw new InvalidOperationException("ExerciseCatalogService no registrado"),
        App.GetService<DatabaseService>()
            ?? throw new InvalidOperationException("DatabaseService no registrado"))
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
        _ = WarmUpCacheAsync();
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

    #region Partial callbacks

    partial void OnSearchTextChanged(string value)
    {
        _liveCts?.Cancel();
        _liveCts?.Dispose();
        _liveCts = new CancellationTokenSource();
        _ = LiveFilterAsync(value, _liveCts.Token);
    }

    #endregion

    #region Private methods

    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

    private async Task WarmUpCacheAsync()
    {
        try
        {
            var count = await _databaseService.GetExerciseCountAsync();
            var lastTicks = Preferences.Get("ExerciseCacheDateTicks", 0L);
            var isStale = lastTicks == 0L || (DateTime.UtcNow.Ticks - lastTicks) > CacheTtl.Ticks;

            if (count > 0 && !isStale) return;

            var all = await _exerciseCatalogService.GetAllExercisesAsync();
            if (all.Count > 0)
            {
                await _databaseService.BulkInsertExercisesAsync(all);
                Preferences.Set("ExerciseCacheDateTicks", DateTime.UtcNow.Ticks);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ Error calentando caché de ejercicios: {ex.Message}");
        }
    }

    private async Task LiveFilterAsync(string query, CancellationToken ct)
    {
        if (!IsCacheWarm()) return;

        try
        {
            List<ExerciseSummaryDto> results;
            bool isFiltered;

            if (query.Length >= 2)
            {
                results = await _databaseService.SearchExercisesAsync(query, limit: PageSize);
                isFiltered = true;
            }
            else
            {
                results = await _databaseService.GetCachedExercisesAsync(0, PageSize);
                isFiltered = false;
            }

            if (ct.IsCancellationRequested) return;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (ct.IsCancellationRequested) return;
                _currentPage = 1;
                Exercises.Clear();
                foreach (var ex in results) Exercises.Add(ex);
                HasMorePages = !isFiltered && results.Count == PageSize;
                IsEmpty = Exercises.Count == 0;
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ Error en filtro en vivo: {ex.Message}");
        }
    }

    private static bool IsCacheWarm()
    {
        var lastTicks = Preferences.Get("ExerciseCacheDateTicks", 0L);
        return lastTicks != 0L && (DateTime.UtcNow.Ticks - lastTicks) <= CacheTtl.Ticks;
    }

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

            // When cache is warm and no filter is active, serve from SQLite (instant)
            if (muscle == null && IsCacheWarm())
            {
                var cached = await _databaseService.GetCachedExercisesAsync(_currentPage, PageSize);
                if (cached.Count > 0)
                {
                    foreach (var exercise in cached)
                        Exercises.Add(exercise);
                    _currentPage++;
                    HasMorePages = cached.Count == PageSize;
                    IsEmpty = false;
                }
                else
                {
                    HasMorePages = false;
                    IsEmpty = Exercises.Count == 0;
                }
                return;
            }

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
