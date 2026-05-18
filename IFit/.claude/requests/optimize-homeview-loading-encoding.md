# Request: Optimizar carga de HomeView y corregir encoding

## Contexto
HomeViewModel / HomeView presentan tres problemas detectados en revisión:
1. Caracteres corruptos en strings literales del carrusel informativo.
2. Estado `DoesntHaveRoutine` y `StatusMessage` no se reinician al re-inicializar, causando flash de UI stale.
3. Carga secuencial innecesaria: `getLatestActiveRoutineByUserIdAsync` obtiene **todas** las rutinas activas para luego quedarse solo con la de mayor Id. Si `CurrentRoutineId` ya está en Preferences (guardado tras crear la rutina), la primera llamada HTTP se puede evitar.
4. HomeView.xaml no tiene `LoadingOverlay`; oculta el contenido con `InvertedBoolConverter` pero no muestra ningún spinner al usuario.

---

## Tarea 1 — `/arch-review` → `ViewModels/HomeViewModel.cs`

### 1a. Fix encoding de caracteres (líneas 22–31)

Reemplazar los caracteres corruptos (ISO-8859-1 embebidos en UTF-8) por sus equivalentes Unicode correctos:

| Texto actual (corrupto) | Texto corregido |
|---|---|
| `nutrici\xF3n` | `nutrición` |
| `Mejor despu\xE9s` | `Mejor después` |
| `m\xE1ximo` | `máximo` |

### 1b. Reset de estado stale al comienzo de `InitializeAsync`

Añadir al inicio del método, antes de cualquier llamada async:

```csharp
private async Task InitializeAsync()
{
    try
    {
        DoesntHaveRoutine = false;   // evita flash de card "sin rutina" de sesión anterior
        StatusMessage = "Cargando tu rutina actual...";
        // ... resto igual
```

### 1c. Optimización: evitar fetch completo si ya tenemos el routineId en Preferences

`TrainingService.getLatestActiveRoutineByUserIdAsync` hace GET `/routines/user/{userId}/active`
y carga **todas** las rutinas activas para quedarse solo con la de mayor Id.

Si `CurrentRoutineId` ya está en Preferences (lo escribe `TrainingService.createRoutineAsync`),
usar directamente `getRoutineByIdAsync(currentRoutineId)` para una única llamada:

```csharp
var userId = Preferences.Get("UserId", 0L);
var coachName = Preferences.Get("CoachModelTypeName", "tu coach");
ButtonContent = "Pregunta a " + coachName;

var cachedRoutineId = Preferences.Get("CurrentRoutineId", 0L);
if (cachedRoutineId > 0)
{
    Routine = await _trainingService.getRoutineByIdAsync(cachedRoutineId);
}
else
{
    Routine = await _trainingService.getLatestActiveRoutineByUserIdAsync(userId);
    if (Routine?.Id != null)
        Preferences.Set("CurrentRoutineId", Routine.Id);
}
```

Si la llamada al Id cacheado devuelve `null` (rutina borrada/desactivada), caer al fallback de todas las activas.

---

## Tarea 2 — `/ui-review` → `Views/HomeView.xaml`

### 2a. Añadir `LoadingOverlay`

Actualmente no hay spinner visible cuando `IsLoading = true`: el contenido desaparece pero
la pantalla queda en blanco. Añadir al final del Grid raíz (después de `HomeFooterView`):

```xml
<components:LoadingOverlay
    Grid.RowSpan="3"
    IsVisible="{Binding IsLoading}"
    StatusMessage="{Binding StatusMessage}" />
```

Con namespace `xmlns:components="clr-namespace:IFit.Views.Components"`.

### 2b. Padding lateral incorrecto

El Grid central (`Grid.Row="1"`) tiene `Margin="15"`. El design system establece **20 dp** laterales.
Cambiar a `Margin="20,0"` (o el valor equivalente que ya usan otras vistas).

---

## Archivos a modificar

| Archivo | Agente | Cambios |
|---|---|---|
| `ViewModels/HomeViewModel.cs` | `/arch-review` | Fix encoding, reset state, optimización Preferences |
| `Views/HomeView.xaml` | `/ui-review` | LoadingOverlay + Margin 20 dp |

---

## Criterios de aceptación

- [ ] Los strings del carrusel se muestran correctamente en español (ó, é, á) sin `?` ni caracteres extraños.
- [ ] Al volver a HomeView tras una sesión con "sin rutina", no aparece el banner amarillo mientras carga.
- [ ] Si el usuario ya tiene una rutina creada, la carga de Home hace **1 llamada HTTP** en vez de 2.
- [ ] El spinner (`LoadingOverlay`) es visible mientras `IsLoading = true`.
