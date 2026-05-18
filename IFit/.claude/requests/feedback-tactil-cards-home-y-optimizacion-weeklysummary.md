# Request: Feedback táctil en cards HomeView + optimización navegación WeeklySummary

## Contexto
Los dos cards interactivos de HomeView (entrenamiento de hoy y resumen semanal) usan
`TapGestureRecognizer` sin ningún feedback visual al pulsar, lo que hace que la UI parezca
congelada. Además, WeeklySummaryViewModel realiza llamadas HTTP redundantes que ya podrían
evitarse pasando el objeto `Routine` desde HomeViewModel, y tiene dos bugs de navegación.

---

## Tarea 1 — `/arch-review` → ViewModels

### Archivos: `HomeViewModel.cs` + `WeeklySummaryViewModel.cs`

---

### 1a. Pasar `Routine` como `QueryProperty` a WeeklySummaryView

**Problema**: `WeeklySummaryViewModel.InitializeAsync` llama a
`getLatestActiveRoutineByUserIdAsync` aunque `HomeViewModel` ya tiene `Routine` en memoria.
Esto provoca una llamada HTTP adicional al entrar a WeeklySummary → la pantalla tarda en mostrar
contenido.

**Además**: `findUserById` se llama en paralelo pero su resultado (`currentUser`) solo se usa
para comprobar que no es null — el objeto no se usa en ningún otro sitio. Es una llamada HTTP
desperdiciada.

**Fix en `HomeViewModel.GoToWeeklySummaryAsync`** — pasar Routine:
```csharp
[RelayCommand]
public async Task GoToWeeklySummaryAsync()
{
    if (Routine == null) return; // sin rutina no hay nada que mostrar

    var navigationParameter = new Dictionary<string, object>()
    {
        { "Routine", Routine }
    };
    await Shell.Current.GoToAsync("WeeklySummaryView", navigationParameter);
}
```

**Fix en `WeeklySummaryViewModel`** — recibir la Routine y eliminar las llamadas innecesarias:
```csharp
[QueryProperty(nameof(Routine), "Routine")]
public partial class WeeklySummaryViewModel : ObservableObject
{
    // ...

    partial void OnRoutineChanged(RoutineResponseDto? value)
    {
        if (value is null) return;
        BuildSessionLists();
        IsLoading = false;
    }

    private async Task InitializeAsync()
    {
        // Solo se ejecuta si Routine no fue recibida por QueryProperty (acceso directo a la ruta)
        IsLoading = true;
        StatusMessage = "Cargando tu rutina...";

        var userId = Preferences.Get("UserId", 0L);
        if (userId == 0) { IsLoading = false; return; }

        var cachedRoutineId = Preferences.Get("CurrentRoutineId", 0L);
        Routine = cachedRoutineId > 0
            ? await _trainingService.getRoutineByIdAsync(cachedRoutineId)
            : await _trainingService.getLatestActiveRoutineByUserIdAsync(userId);

        // BuildSessionLists y IsLoading = false los gestiona OnRoutineChanged
        if (Routine == null)
        {
            StatusMessage = "No se ha encontrado la rutina actual.";
            IsLoading = false;
        }
    }
```

`AppearingAsync` llama a `InitializeAsync` solo si `Routine == null` al aparecer:
```csharp
[RelayCommand]
public async Task AppearingAsync()
{
    if (_isInitialized) return;
    _isInitialized = true;

    if (Routine != null)
    {
        // Ya llegó por QueryProperty: solo construir listas
        BuildSessionLists();
        IsLoading = false;
        return;
    }

    await InitializeAsync();
}
```

---

### 1b. Reset de `TrainingDayDto` tras navegar a TrainingDayDetailView

**Problema**: `OnTrainingDayDtoChanged` dispara navegación cuando el `SelectedItem` del
`CollectionView` cambia. Tras volver de `TrainingDayDetailView`, la propiedad
`TrainingDayDto` conserva el último valor seleccionado. Si el usuario pulsa el mismo día,
el valor no cambia → `OnTrainingDayDtoChanged` no se dispara → parece que la app está pillada.

**Fix**: resetear `TrainingDayDto = null` después de navegar:
```csharp
private async Task NavigateToDetailAsync(TrainingDayDto value)
{
    var navigationParameter = new Dictionary<string, object>()
    {
        { "Routine", Routine },
        { "TrainingDay", value }
    };
    await Shell.Current.GoToAsync("TrainingDayDetailView", navigationParameter);
    TrainingDayDto = null; // permite re-seleccionar el mismo día al volver
}
```

---

## Tarea 2 — `/ui-review` → Views/HomeView.xaml + Views/WeeklySummaryView.xaml

---

### 2a. Efecto de pulsación en los cards de HomeView (patrón footer)

**Problema**: los cards de HomeView usan `TapGestureRecognizer`, que en Android no produce
ningún feedback visual. El `HomeFooterView` usa el patrón correcto: un `Button` transparente
superpuesto que activa el ripple nativo de Android.

**Patrón a aplicar** (ya usado en `HomeFooterView`):
Envolver el contenido del `Border` del card en un `Grid`, y añadir un `Button` trasparente
encima que ocupa todo el espacio con `Grid.RowSpan` o `AbsoluteLayout`:

```xml
<!-- Card entrenamiento de hoy -->
<Border Grid.Row="1" ... >
    <!-- ... contenido existente del card ... -->

    <!-- Botón invisible superpuesto para ripple nativo -->
    <Button BackgroundColor="Transparent"
            BorderColor="Transparent"
            HorizontalOptions="Fill"
            VerticalOptions="Fill"
            Command="{Binding OpenTrainingDayDetailCommand}" />
</Border>
```

Eliminar el `TapGestureRecognizer` de ambos cards y sustituirlo por este patrón.
Aplicar igual al card de "Resumen de la semana" con `GoToWeeklySummaryCommand`.

**Nota**: `Border` en MAUI acepta un único hijo. Para superponer el Button sin layout adicional,
usar `Grid` sin definiciones de fila/columna (todos los hijos en la misma celda):

```xml
<Border Grid.Row="1" StrokeThickness="0" ... >
    <Border.StrokeShape>
        <RoundRectangle CornerRadius="8"/>
    </Border.StrokeShape>

    <Grid>
        <!-- Contenido del card (tu Grid existente con columnas) -->
        <Grid>
            <Grid.ColumnDefinitions> ... </Grid.ColumnDefinitions>
            <!-- barra gradiente + contenido -->
        </Grid>

        <!-- Botón transparente encima -->
        <Button BackgroundColor="Transparent"
                BorderColor="Transparent"
                HorizontalOptions="Fill"
                VerticalOptions="Fill"
                Command="{Binding OpenTrainingDayDetailCommand}" />
    </Grid>
</Border>
```

---

### 2b. `LoadingOverlay` en WeeklySummaryView

**Problema**: `WeeklySummaryView` oculta el contenido con `InvertedBoolConverter` cuando
`IsLoading = true`, pero no muestra ningún spinner. La pantalla queda en negro.

Añadir namespace y overlay al final del Grid raíz:
```xml
xmlns:components="clr-namespace:IFit.Views.Components"

<!-- Al final del Grid raíz, después del ScrollView -->
<components:LoadingOverlay
    Grid.RowSpan="2"
    IsLoading="{Binding IsLoading}"
    Message="{Binding StatusMessage}" />
```

---

## Archivos a modificar

| Archivo | Agente | Cambios |
|---|---|---|
| `ViewModels/HomeViewModel.cs` | `/arch-review` | GoToWeeklySummaryAsync pasa Routine |
| `ViewModels/WeeklySummaryViewModel.cs` | `/arch-review` | QueryProperty Routine, eliminar findUserById, reset TrainingDayDto, AppearingAsync optimizado |
| `Views/HomeView.xaml` | `/ui-review` | Botón transparente en ambos cards (efecto ripple) |
| `Views/WeeklySummaryView.xaml` | `/ui-review` | LoadingOverlay |

---

## Criterios de aceptación

- [ ] Pulsar el card de entrenamiento de hoy muestra feedback visual (ripple Android) antes de navegar.
- [ ] Pulsar el card de resumen semanal muestra feedback visual antes de navegar.
- [ ] Al entrar a WeeklySummaryView desde HomeView no se realiza ninguna llamada HTTP (Routine ya viene del parámetro).
- [ ] Desde WeeklySummaryView se puede pulsar el mismo día dos veces seguidas y navega correctamente ambas.
- [ ] WeeklySummaryView muestra spinner mientras carga en vez de pantalla negra.
