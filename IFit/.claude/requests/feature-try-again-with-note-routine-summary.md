# Request: Feature — "Probar de nuevo" con nota al coach en RoutineSummaryView

## Descripción

Simplificar la fila de botones de `RoutineSummaryView` eliminando "Editar cuestionario"
y añadir un overlay de texto libre en "Probar de nuevo" para que el usuario pueda
indicarle al modelo qué quiere ajustar antes de regenerar la rutina.

---

## Cambios de UI — layout final deseado (Row 4)

```
┌────────────────────────────────────┐
│  [ Probar de nuevo ]  (blanco, 1/2 width)
│  [ ¡Me gusta!      ]  (amarillo, full width)
└────────────────────────────────────┘
```

- Eliminar la columna de "Editar cuestionario" por completo.
- "Probar de nuevo" queda solo en la mitad izquierda (o puede ser full width según diseño final).
- "¡Me gusta!" pasa a la fila inferior, full width.
- Estilo actual de cada botón se mantiene.

---

## Overlay de texto — "Probar de nuevo"

Al pulsar "Probar de nuevo" se muestra un overlay igual en diseño al que usa
`AppUserQuestionnaireView` para el `AdditionalText`:

```
┌───────────────────────────────────────┐
│  ¿Qué quieres cambiar en tu rutina?   │  ← Label título
│  ┌─────────────────────────────────┐  │
│  │  Ej: más pecho, menos pierna… │  │  ← Entry con placeholder
│  └─────────────────────────────────┘  │
│  [ Cancelar ]    [ Regenerar rutina ] │
└───────────────────────────────────────┘
```

- El texto es **opcional**: si el usuario deja el campo vacío y pulsa "Regenerar rutina",
  se regenera sin nota (mismo comportamiento que el "Probar de nuevo" actual).
- "Cancelar" cierra el overlay sin navegar ni regenerar.
- El overlay es un `Grid` con `ZIndex` alto, igual que `LoadingOverlay`.
- Binding: `IsVisible="{Binding ShowNoteInput}"`.

---

## Archivos afectados — Cliente

### `Views/RoutineSummaryView.xaml`
- Eliminar `<Button Command="{Binding NavigateToLastQuestionCommand}" ... Text="Editar cuestionario" />`
- Simplificar el `Grid ColumnDefinitions="*,*"` a un solo botón "Probar de nuevo" (o full width)
- Añadir overlay de texto (referencia visual: `AppUserQuestionnaireView.xaml`, región del Entry adicional)
- Bindings nuevos: `ShowNoteInput`, `UserNote`, `ConfirmTryAgainCommand`, `CancelTryAgainCommand`

### `ViewModels/RoutineSummaryViewModel.cs`

**Inyectar `AIRoutineService`** en el constructor (actualmente solo tiene `TrainingService` y `AppUserService`).

**Campos nuevos:**
```csharp
[ObservableProperty]
private bool _showNoteInput = false;

[ObservableProperty]
private string _userNote = string.Empty;
```

**Reemplazar `TryAgainCommand`:**
```csharp
// ANTES: navega con ".."
[RelayCommand]
private async Task TryAgainAsync() => await Shell.Current.GoToAsync("..");

// DESPUÉS: abre el overlay
[RelayCommand]
private void TryAgain() => ShowNoteInput = true;
```

**Nuevos comandos:**
```csharp
[RelayCommand]
private void CancelTryAgain()
{
    ShowNoteInput = false;
    UserNote = string.Empty;
}

[RelayCommand]
private async Task ConfirmTryAgainAsync()
{
    ShowNoteInput = false;
    IsLoading = true;
    StatusMessage = "Regenerando tu rutina...";

    try
    {
        long userId = Preferences.Get("UserId", 0L);
        long responseId = Preferences.Get("responseId", 0L);
        string coachType = Preferences.Get("CoachModelTypeName", string.Empty);
        string? note = string.IsNullOrWhiteSpace(UserNote) ? null : UserNote.Trim();

        var newRoutine = await _aiRoutineService.GenerateRoutineAsync(
            userId.ToString(), responseId, coachType, note);

        if (newRoutine == null)
        {
            await NotificationService.ShowErrorAsync("No se pudo regenerar la rutina. Intenta de nuevo.");
            return;
        }

        Routine = newRoutine;   // el binding actualiza la vista automáticamente
        UserNote = string.Empty;
        await NotificationService.ShowSuccessAsync("¡Rutina regenerada!");
    }
    catch (Exception ex)
    {
        await NotificationService.ShowErrorAsync($"Error al regenerar: {ex.Message}");
    }
    finally
    {
        IsLoading = false;
        StatusMessage = string.Empty;
    }
}
```

**Eliminar `NavigateToLastQuestionCommand` completo.**

### `Models/Dtos/Routine/RoutineDtos.cs`

Renombrar `userNote` → `note` para alinear con el contrato del servidor:
```csharp
[JsonPropertyName("note")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public string? Note { get; set; }
```

### `Services/AIRoutineService.cs`

Añadir parámetro `note` a `GenerateRoutineAsync`:
```csharp
public async Task<RoutineResponseDto?> GenerateRoutineAsync(
    string userId, long responseId, string? coachType = null, string? note = null)
```

Y pasarlo al request DTO:
```csharp
var request = new GenerateRoutineRequestDto
{
    UserId = userId,
    ResponseId = responseId,
    CoachType = string.IsNullOrWhiteSpace(coachType) ? null : coachType.ToUpper().Trim(),
    Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
};
```

En `ConfirmTryAgainAsync` del ViewModel, pasar `note` al servicio:
```csharp
string? note = string.IsNullOrWhiteSpace(UserNote) ? null : UserNote.Trim();
var newRoutine = await _aiRoutineService.GenerateRoutineAsync(
    userId.ToString(), responseId, coachType, note);
```

---

## Contrato Backend — confirmado

**Endpoint:** `POST /ifit/api/v1/routines/generate`

**Request sin nota** (usuario acepta sin observaciones):
```json
{
  "userId": 1,
  "responseId": 3,
  "coachType": "RONNIE",
  "note": null
}
```

**Request con nota** (usuario rechaza y quiere ajustes):
```json
{
  "userId": 1,
  "responseId": 3,
  "coachType": "RONNIE",
  "note": "No quiero ejercicios de sentadilla, tengo molestias en la rodilla derecha"
}
```

**Comportamiento servidor:**
- `note` null o vacío → genera igual que antes, sin cambios en el prompt.
- `note` con contenido → se adjunta al prompt bajo la sección `NOTA DEL USUARIO`,
  que el modelo tiene en cuenta para adaptar la rutina.

**Response:** mismo `RoutineResponseDto` de siempre. Sin campos nuevos en la respuesta.

**Cambios backend ya implementados** — el cliente solo necesita enviar el campo `note`.

---

## Checklist de verificación post-implementación

1. ✅ "Editar cuestionario" desaparece de la UI.
2. ✅ "Probar de nuevo" abre el overlay con el Entry.
3. ✅ "Cancelar" en el overlay cierra sin regenerar.
4. ✅ Confirmar con nota vacía → regenera igual que antes (sin nota).
5. ✅ Confirmar con nota → rutina regenerada incluye la nota en el resultado.
6. ✅ "¡Me gusta!" sigue guardando la rutina correctamente.
7. ✅ `IsLoading` bloquea ambos botones durante la regeneración.
8. ✅ `RoutineSummaryView` sigue siendo ShellContent — el `Routine` se actualiza
   por binding al asignar `Routine = newRoutine` en el VM.
