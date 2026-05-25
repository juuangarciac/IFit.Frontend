using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFit.Models.Dtos.AI;
using IFit.Models.Dtos.Exercise;
using IFit.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace IFit.ViewModels;

/// <summary>Ejercicio añadido manualmente a un día de entrenamiento.</summary>
public partial class ManualExerciseEntry : ObservableObject
{
    public string ExerciseName { get; init; } = string.Empty;

    [ObservableProperty]
    public partial string Sets { get; set; } = "3";

    [ObservableProperty]
    public partial string Reps { get; set; } = "10";

    [ObservableProperty]
    public partial string RestSeconds { get; set; } = "60";
}

/// <summary>Día de entrenamiento en construcción.</summary>
public partial class ManualDayEntry : ObservableObject
{
    public int DayNumber { get; init; }

    [ObservableProperty]
    public partial string DayName { get; set; } = string.Empty;

    public ObservableCollection<ManualExerciseEntry> Exercises { get; } = new();
}

public partial class ManualRoutineBuilderViewModel : ObservableObject
{
    #region Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStep1))]
    [NotifyPropertyChangedFor(nameof(IsStep2))]
    [NotifyPropertyChangedFor(nameof(StepIndicatorText))]
    public partial int Step { get; set; } = 1;

    public bool IsStep1 => Step == 1;
    public bool IsStep2 => Step == 2;
    public string StepIndicatorText => $"PASO {Step} DE 2";

    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TrainingDaysText))]
    public partial int TrainingDays { get; set; } = 3;

    public string TrainingDaysText => TrainingDays.ToString();

    [ObservableProperty]
    public partial ObservableCollection<ManualDayEntry> Days { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentDay))]
    [NotifyPropertyChangedFor(nameof(DayIndicatorText))]
    public partial int CurrentDayIndex { get; set; } = 0;

    public ManualDayEntry? CurrentDay => Days.ElementAtOrDefault(CurrentDayIndex);
    public string DayIndicatorText => Days.Count > 0 ? $"Día {CurrentDayIndex + 1} de {Days.Count}" : string.Empty;
    public bool HasMultipleDays => Days.Count > 1;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<ExerciseSummaryDto> SearchResults { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowExerciseList))]
    public partial bool IsSearching { get; set; }

    /// <summary>Muestra la lista de ejercicios del día cuando no hay búsqueda activa.</summary>
    public bool ShowExerciseList => !IsSearching;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSaving { get; set; }

    #endregion

    #region Services

    private readonly ExerciseCatalogService _exerciseCatalogService;
    private readonly TrainingService _trainingService;

    #endregion

    #region Constructor

    public ManualRoutineBuilderViewModel(
        ExerciseCatalogService exerciseCatalogService,
        TrainingService trainingService)
    {
        _exerciseCatalogService = exerciseCatalogService
            ?? throw new ArgumentNullException(nameof(exerciseCatalogService));
        _trainingService = trainingService
            ?? throw new ArgumentNullException(nameof(trainingService));
    }

    public ManualRoutineBuilderViewModel() : this(
        App.GetService<ExerciseCatalogService>()
            ?? throw new InvalidOperationException("ExerciseCatalogService no registrado"),
        App.GetService<TrainingService>()
            ?? throw new InvalidOperationException("TrainingService no registrado"))
    { }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task GoBackAsync()
    {
        if (Step == 2)
        {
            ClearSearch();
            Step = 1;
            return;
        }
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private void IncreaseDays()
    {
        if (TrainingDays < 7) TrainingDays++;
    }

    [RelayCommand]
    private void DecreaseDays()
    {
        if (TrainingDays > 1) TrainingDays--;
    }

    [RelayCommand]
    private void Continue()
    {
        if (string.IsNullOrWhiteSpace(Description))
            Description = "Mi rutina personalizada";

        // Reconstruir los días solo si el número ha cambiado
        if (Days.Count != TrainingDays)
        {
            Days.Clear();
            for (int i = 1; i <= TrainingDays; i++)
            {
                Days.Add(new ManualDayEntry
                {
                    DayNumber = i,
                    DayName = $"Día {i}"
                });
            }
        }

        CurrentDayIndex = 0;
        OnPropertyChanged(nameof(CurrentDay));
        OnPropertyChanged(nameof(DayIndicatorText));
        OnPropertyChanged(nameof(HasMultipleDays));
        Step = 2;
    }

    [RelayCommand]
    private void GoToPreviousDay()
    {
        if (Days.Count == 0) return;
        ClearSearch();
        CurrentDayIndex = CurrentDayIndex == 0 ? Days.Count - 1 : CurrentDayIndex - 1;
        OnPropertyChanged(nameof(CurrentDay));
        OnPropertyChanged(nameof(DayIndicatorText));
    }

    [RelayCommand]
    private void GoToNextDay()
    {
        if (Days.Count == 0) return;
        ClearSearch();
        CurrentDayIndex = (CurrentDayIndex + 1) % Days.Count;
        OnPropertyChanged(nameof(CurrentDay));
        OnPropertyChanged(nameof(DayIndicatorText));
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            ClearSearch();
            return;
        }

        IsLoading = true;
        StatusMessage = "Buscando...";

        try
        {
            var result = await _exerciseCatalogService.GetExercisesAsync(
                page: 0, size: 20, muscle: SearchText.Trim());

            SearchResults.Clear();

            if (result?.Content != null && result.Content.Count > 0)
            {
                foreach (var ex in result.Content)
                    SearchResults.Add(ex);

                IsSearching = true;
                StatusMessage = string.Empty;
            }
            else
            {
                IsSearching = true; // Mostramos el panel de resultados vacío
                StatusMessage = "Sin resultados para ese término.";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ Error buscando ejercicios: {ex.Message}");
            IsSearching = false;
            StatusMessage = "Error en la búsqueda.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        SearchResults.Clear();
        IsSearching = false;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private void AddExercise(ExerciseSummaryDto exercise)
    {
        if (exercise is null || CurrentDay is null) return;

        // Evitar duplicados en el mismo día
        if (CurrentDay.Exercises.Any(e => e.ExerciseName == exercise.Name))
        {
            ClearSearch();
            return;
        }

        CurrentDay.Exercises.Add(new ManualExerciseEntry
        {
            ExerciseName = exercise.Name
        });

        ClearSearch();
    }

    [RelayCommand]
    private void RemoveExercise(ManualExerciseEntry exercise)
    {
        if (exercise is null || CurrentDay is null) return;
        CurrentDay.Exercises.Remove(exercise);
    }

    [RelayCommand]
    private async Task SaveRoutineAsync()
    {
        // Validar que todos los días tienen al menos un ejercicio
        var emptyDays = Days.Where(d => d.Exercises.Count == 0).ToList();
        if (emptyDays.Count > 0)
        {
            var names = string.Join(", ", emptyDays.Select(d => d.DayName));
            await NotificationService.ShowErrorAsync($"Añade al menos un ejercicio a: {names}");
            return;
        }

        try
        {
            IsSaving = true;

            long userId = Preferences.Get("UserId", 0L);

            var activeRoutine = await _trainingService.getLatestActiveRoutineByUserIdAsync(userId);
            if (activeRoutine != null)
                await NotificationService.ShowInfoAsync("Al guardar esta rutina, tu rutina actual quedará desactivada automáticamente.");

            var routineDto = new RoutineResponseDto
            {
                Description = Description,
                TrainingDays = TrainingDays,
                Days = Days.Select(d => new TrainingDayDto
                {
                    DayNumber = d.DayNumber,
                    DayName = string.IsNullOrWhiteSpace(d.DayName) ? $"Día {d.DayNumber}" : d.DayName,
                    Description = string.Empty,
                    Exercises = d.Exercises.Select((e, i) => new ExerciseDto
                    {
                        ExerciseName = e.ExerciseName,
                        Sets = int.TryParse(e.Sets, out int s) ? s : 3,
                        Reps = string.IsNullOrWhiteSpace(e.Reps) ? "10" : e.Reps,
                        RestSeconds = int.TryParse(e.RestSeconds, out int r) ? r : 60,
                        Notes = string.Empty,
                        OrderIndex = i
                    }).ToList()
                }).ToList()
            };

            var response = await _trainingService.createRoutineAsync(userId, routineDto);

            if (response == null)
            {
                await NotificationService.ShowErrorAsync("No se pudo guardar la rutina. Inténtalo de nuevo.");
                return;
            }

            await NotificationService.ShowSuccessAsync("¡Rutina guardada correctamente!");
            await Shell.Current.GoToAsync("///HomeView", false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"✗ Error guardando rutina manual: {ex.Message}");
            await NotificationService.ShowErrorAsync("Error inesperado al guardar la rutina.");
        }
        finally
        {
            IsSaving = false;
        }
    }

    #endregion
}
