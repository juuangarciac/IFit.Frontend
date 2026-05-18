# Request: Botón "Editar cuestionario" desde RoutineSummaryView

## Descripción
Añadir un botón en `RoutineSummaryView` que navegue al usuario de vuelta a la última pregunta
del cuestionario completado, reutilizando la sesión existente mediante `QuestionnaireService.GoToPreviousQuestion`.

## Análisis previo

**Flujo real del stack** hasta `RoutineSummaryView`:
```
AppUserQuestionnaireView (RegisterRoute)
  → push → QuestionnaireSummaryView (RegisterRoute)
    → "//RoutineSummaryView" ← reset de stack (ShellContent)
```

`QuetionnaireSummaryViewModel:149` hace `GoToAsync("//RoutineSummaryView", params)`, lo que **reinicia el stack**.
El `TryAgainAsync()` actual hace `GoToAsync("..")` que no tiene stack al que volver (comportamiento indefinido desde un ShellContent).

**Estado disponible en Preferences al llegar a `RoutineSummaryView`:**

| Clave | Quién la escribió | Disponible |
|---|---|---|
| `UserId` | `VerificationViewModel` | ✅ |
| `CoachId` | `CoachModelTypeSelectionViewModel` | ✅ |
| `ExperienceLevelId` | `ExperienceLevelViewModel` | ✅ |
| `responseId` | `AppUserQuestionnaireViewModel.OnQuestionnaireCompleted()` | ✅ |

**Servicio clave existente:** `QuestionnaireService.GoToPreviousQuestion(responseId)` →
endpoint `POST /questionnaires/responses/{responseId}/previous`.
No se necesita ningún servicio nuevo.

---

## Flujo de trabajo por agentes

### `[AGENTE: arch-review]` — ViewModel y navegación

**Archivos:** `ViewModels/RoutineSummaryViewModel.cs`, `ViewModels/AppUserQuestionnaireViewModel.cs`

**Cambio 1 — `RoutineSummaryViewModel.cs`:**
Añadir nuevo `[RelayCommand]` `NavigateToLastQuestionAsync`:

```csharp
[RelayCommand]
private async Task NavigateToLastQuestionAsync()
{
    try
    {
        long responseId = Preferences.Get("responseId", 0L);
        if (responseId <= 0)
        {
            await NotificationService.ShowErrorAsync("No se encontró una sesión de cuestionario previa.");
            return;
        }

        var navigationParams = new Dictionary<string, object>
        {
            { "ResumeResponseId", responseId }
        };
        await Shell.Current.GoToAsync("AppUserQuestionnaireView", navigationParams);
    }
    catch (Exception ex)
    {
        await ErrorHandler.HandleErrorAsync($"Error al navegar al cuestionario: {ex.Message}");
    }
}
```

**Restricciones:**
- Usar `"AppUserQuestionnaireView"` sin prefijo `//` — es RegisterRoute.
- No modificar Preferences en este método.

---

**Cambio 2 — `AppUserQuestionnaireViewModel.cs`:**

Añadir `[QueryProperty]` y soporte de modo reanudación. El setter dispara `InitializeAsync`
en lugar del constructor (el QueryProperty se asigna después del constructor en MAUI Shell).

```csharp
// Campo y QueryProperty:
private long _resumeResponseId;
private bool _initializationStarted = false;

[QueryProperty(nameof(ResumeResponseId), "ResumeResponseId")]  // ← añadir a la clase
public long ResumeResponseId
{
    get => _resumeResponseId;
    set
    {
        _resumeResponseId = value;
        _ = InitializeAsync(); // Shell asigna QueryProperties después del constructor
    }
}
```

Eliminar `_ = InitializeAsync()` del constructor sin parámetros y añadir llamada en el setter.
Añadir guard en `InitializeAsync()`:

```csharp
private async Task InitializeAsync()
{
    if (_initializationStarted) return;
    _initializationStarted = true;

    try
    {
        IsLoading = true;
        StatusMessage = "Personalizando cuestionario...";

        if (_userId <= 0) throw new InvalidOperationException("UserId no válido.");
        if (_coachId <= 0) throw new InvalidOperationException("CoachId no válido.");
        if (_experienceLevelId <= 0) throw new InvalidOperationException("ExperienceLevelId no válido.");

        var questionnaireDto = await _questionnaireService
            .GetQuestionnaireByCoachIdAndExperienceLevelId(_coachId, _experienceLevelId);

        if (questionnaireDto == null)
            throw new InvalidOperationException("No se encontró cuestionario para este coach y nivel.");

        _questionnaireId = questionnaireDto.Id;
        TotalQuestions = questionnaireDto.Questions?.Count ?? 0;

        QuestionnaireResponseDTO? response;

        if (_resumeResponseId > 0)
        {
            // MODO REANUDACIÓN: posicionarse en la última pregunta de la sesión completada
            StatusMessage = "Cargando última pregunta...";
            _responseId = _resumeResponseId;
            response = await _questionnaireService.GoToPreviousQuestion(_responseId);

            if (response == null)
            {
                // Fallback: sesión completada no permite retroceder → iniciar nueva
                Debug.WriteLine("Sesión completada sin retroceso disponible, iniciando nueva sesión.");
                StatusMessage = "Iniciando nuevo cuestionario...";
                response = await _questionnaireService.StartQuestionnaire(_userId, _questionnaireId);
                if (response != null) _responseId = response.ResponseId;
            }
        }
        else
        {
            // MODO NORMAL: iniciar sesión nueva desde la primera pregunta
            response = await _questionnaireService.StartQuestionnaire(_userId, _questionnaireId);
            if (response != null) _responseId = response.ResponseId;
        }

        if (response == null)
        {
            await NotificationService.ShowErrorAsync("No se pudo iniciar el cuestionario.");
            return;
        }

        if (response.CurrentQuestion != null)
        {
            CurrentQuestion = response.CurrentQuestion;
            CurrentQuestionIndex = response.TotalQuestionsAnswered > 0
                ? response.TotalQuestionsAnswered - 1
                : 0;
        }
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error en InitializeAsync: {ex.Message}");
        await NotificationService.ShowErrorAsync("No se pudo cargar el cuestionario.");
    }
    finally
    {
        IsLoading = false;
        StatusMessage = string.Empty;
        UpdateNavigationState();
    }
}
```

**Restricciones — no romper:**
- `GoBackAsync` con `CurrentQuestionIndex <= 0` sigue haciendo `GoToAsync("..")`.
- `OnQuestionnaireCompleted` sigue haciendo `Preferences.Set("responseId", _responseId)`.
- El constructor DI con parámetros no cambia.

---

### `[AGENTE: bug-report]` — Riesgos y verificación

**Riesgo 1 — Categoría C (prefijo de navegación):** severidad Alta
Si se usa `"//AppUserQuestionnaireView"` → `Route not found`. Verificar ruta relativa sin prefijo.

**Riesgo 2 — Categoría A/B (QueryProperty asignado después del constructor):** severidad Crítica
Shell asigna QueryProperties **después** del constructor. Si `InitializeAsync()` se llama en el
constructor Y en el setter, se ejecuta dos veces con estados distintos. El guard `_initializationStarted`
previene la doble ejecución. Verificar que el constructor NO llama `_ = InitializeAsync()`.

**Riesgo 3 — Backend sesión completada rechaza `GoToPreviousQuestion`:** severidad Media
Si el backend devuelve `null` para una sesión `COMPLETED`, el fallback arranca desde pregunta 1.
UX degradada pero sin crash. Documentado con `Debug.WriteLine`.

**Riesgo 4 — `TotalQuestions` en 0:** severidad Baja
Si `QuestionnaireDTO.Questions` es null o lista vacía, `ProgressText` muestra `"Pregunta N de 0"`.
Verificar que el endpoint devuelve preguntas embebidas.

**Checklist de verificación:**
1. ✅ Flujo normal onboarding (sin `ResumeResponseId`) → pregunta 1, comportamiento idéntico al actual.
2. ✅ Botón "Editar cuestionario" → llega a la última pregunta (no la primera).
3. ✅ Completar de nuevo desde la última pregunta → `responseId` en Preferences se actualiza.
4. ✅ Presionar botón sin `responseId` en Preferences → toast de error, sin crash.
5. ✅ `GoBackAsync` en última pregunta → pop `..` (comportamiento existente no alterado).
6. ✅ Segundo registro de usuario nuevo → `ResumeResponseId` no llega (flujo normal), sin herencia de sesión anterior.

---

### `[AGENTE: ui-review]` — XAML del nuevo botón

**Archivo:** `Views/RoutineSummaryView.xaml`

**Sustituir el Grid del Row 4 (líneas 199-216) por:**

```xml
<VerticalStackLayout Grid.Row="4"
                     Margin="0,4,0,8"
                     Spacing="10">

    <!-- Fila superior: botones secundarios -->
    <Grid ColumnDefinitions="*,*"
          ColumnSpacing="10">

        <Button Grid.Column="0"
                Command="{Binding TryAgainCommand}"
                CornerRadius="{DynamicResource CornerRadiusRectangle}"
                HorizontalOptions="Fill"
                HeightRequest="52"
                Style="{DynamicResource ButtonBold-XL-dark-white}"
                Text="Probar de nuevo" />

        <Button Grid.Column="1"
                Command="{Binding NavigateToLastQuestionCommand}"
                CornerRadius="{DynamicResource CornerRadiusRectangle}"
                HorizontalOptions="Fill"
                HeightRequest="52"
                IsEnabled="{Binding IsLoading, Converter={StaticResource InvertedBoolConverter}}"
                Style="{DynamicResource ButtonBold-XL-dark-yellow}"
                Text="Editar cuestionario" />
    </Grid>

    <!-- Fila inferior: CTA principal -->
    <Button Command="{Binding SaveRoutineCommand}"
            CornerRadius="{DynamicResource CornerRadiusRectangle}"
            HorizontalOptions="Fill"
            HeightRequest="52"
            IsEnabled="{Binding IsLoading, Converter={StaticResource InvertedBoolConverter}}"
            Style="{DynamicResource ButtonBold-XL-yellow}"
            Text="¡Me gusta!" />

</VerticalStackLayout>
```

**Decisiones de diseño:**
- `ButtonBold-XL-dark-white` → "Probar de nuevo" (secundario neutral).
- `ButtonBold-XL-dark-yellow` → "Editar cuestionario" (énfasis secundario).
- `ButtonBold-XL-yellow` → "¡Me gusta!" permanece como CTA primario en fila propia, ancho completo.
- `HeightRequest="52"` explícito en todos (estándar del design system).
- `IsEnabled` vinculado a `IsLoading` en botones que disparan navegación o guardado.

**Restricciones — no tocar:**
- `onCancelClicked` en code-behind (`RoutineSummaryView.xaml.cs`) → `///MainPage`. No interferir.
- `LoadingOverlay` existente (líneas 19-21). No añadir otro.
- Sin colores ni tamaños inline — solo tokens `StaticResource`/`DynamicResource`.

---

## Resumen de archivos afectados

| Archivo | Agente | Cambio |
|---|---|---|
| `ViewModels/RoutineSummaryViewModel.cs` | arch-review | Nuevo command `NavigateToLastQuestionAsync` |
| `ViewModels/AppUserQuestionnaireViewModel.cs` | arch-review | `[QueryProperty]` + modo reanudación + guard |
| `Views/RoutineSummaryView.xaml` | ui-review | Rediseño Row 4 con tercer botón |
| — | bug-report | Verificación de riesgos y checklist |
