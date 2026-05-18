# Request: Bug — Doble GoToPreviousQuestion y desync cliente/servidor en modo reanudación

## Descripción del bug

Al pulsar "Editar cuestionario" desde `RoutineSummaryView` y volver a `AppUserQuestionnaireView`
en modo reanudación, se producen dos problemas encadenados con la misma causa raíz:

- **Bug 1**: La pregunta mostrada no es la última del cuestionario sino la penúltima (parece "cacheada").
- **Bug 2**: Al responder esa pregunta y pulsar "Siguiente", el servidor rechaza la respuesta
  con "No se pudo guardar tu respuesta". Sin embargo, si el usuario retrocede una pregunta más
  y luego avanza, ambas respuestas se guardan correctamente.

---

## Análisis previo — Causa raíz

### Race condition: doble llamada a `GoToPreviousQuestion`

El constructor sin parámetros llama `_ = InitializeAsync()` (línea 331).
El setter de `ResumeResponseId` resetea el guard y también llama `_ = InitializeAsync()` (línea 266).

**Secuencia real de ejecución:**

```
1. Constructor: _ = InitializeAsync() → _initializationStarted = true
               → await GetQuestionnaireByCoachIdAndExperienceLevelId → SUSPENDE

2. Shell (síncrono): ResumeResponseId setter →
                     _resumeResponseId = value
                     _initializationStarted = false
                     _ = InitializeAsync()   ← SEGUNDA llamada

3. Segunda InitializeAsync: _initializationStarted = true → resume mode →
                             await GoToPreviousQuestion(responseId)  → SUSPENDE

4. Primera InitializeAsync reanuda desde await:
   → _resumeResponseId > 0 es TRUE (ya fue asignado en paso 2)
   → también entra en resume mode →
   → await GoToPreviousQuestion(responseId)  ← SEGUNDA llamada al servidor
```

**Resultado:** `GoToPreviousQuestion` se llama **dos veces concurrentemente** con el mismo `responseId`.

El servidor procesa ambas en secuencia:
- Primera llamada: elimina respuesta N → `TotalQuestionsAnswered = N-1`, devuelve pregunta N
- Segunda llamada: elimina respuesta N-1 → `TotalQuestionsAnswered = N-2`, devuelve pregunta N-1

El cliente muestra la pregunta de la respuesta que llega ÚLTIMA (no determinista),
pero el servidor está **2 posiciones atrás** del estado que el cliente cree tener.

Esto explica ambos bugs:
- Bug 1: el cliente muestra la penúltima pregunta → el usuario cree que es "la de caché"
- Bug 2: el cliente envía `AnswerQuestion` con el `QuestionId` de la pregunta que ve, pero el servidor
  espera la respuesta a una pregunta diferente (posición incorrecta) → 400/422 → "no se pudo guardar"
- Por qué retroceder y avanzar funciona: el `GoBackAsync` del botón resincroniza el servidor
  con el cliente (aunque desplaza la sesión más atrás de lo esperado)

### Bug secundario: `CurrentQuestionIndex` incorrecto en modo reanudación

En `InitializeAsync` modo reanudación (línea 417-419):
```csharp
CurrentQuestionIndex = response.TotalQuestionsAnswered > 0
    ? response.TotalQuestionsAnswered - 1  // ← MAL: -1 de más
    : 0;
```

`GoBackAsync` asigna (línea 561):
```csharp
CurrentQuestionIndex = response.TotalQuestionsAnswered;  // sin -1
```

Ambos reciben el mismo `QuestionnaireResponseDTO` tras un `GoToPreviousQuestion`. La fórmula
correcta en ambos sitios es `response.TotalQuestionsAnswered` (sin restar 1), porque
`TotalQuestionsAnswered` ya representa el índice 0-based de la pregunta actual tras el retroceso.

Con N=3 y `GoToPreviousQuestion` devolviendo `TotalQuestionsAnswered=2`:
- Con `-1`: `CurrentQuestionIndex = 1` → el guard `CurrentQuestionIndex <= 0` de `GoBackAsync`
  permite un retroceso de menos de lo real, y el progress bar muestra posición incorrecta.
- Sin `-1`: `CurrentQuestionIndex = 2` ✅

---

## Archivos afectados

| Archivo | Bug | Líneas |
|---|---|---|
| `ViewModels/AppUserQuestionnaireViewModel.cs` | Race condition init | 331, 264-267, 347-348 |
| `ViewModels/AppUserQuestionnaireViewModel.cs` | `CurrentQuestionIndex` incorrecto | 417-419 |
| Backend: `GoToPreviousQuestion` endpoint | Posible: no resetea `IsCompleted=false` | — |

---

## Flujo de trabajo por agentes

---

### `[AGENTE: bug-report]` — Diagnóstico formal y checklist

**Precondiciones:**
- Usuario autenticado con coach y nivel de experiencia configurados.
- Cuestionario completado al menos una vez en la sesión actual.
- `Preferences["responseId"]` contiene el ID de la sesión completada.

**Pasos para reproducir Bug 1 + Bug 2:**
1. Completar el cuestionario → llegar a `QuestionnaireSummaryView` → generar rutina → llegar a `RoutineSummaryView`.
2. Pulsar "Editar cuestionario".
3. Observar la pregunta mostrada → **Bug 1**: no es la última, es la penúltima.
4. Seleccionar una opción y pulsar "Siguiente" → **Bug 2**: toast "No se pudo guardar tu respuesta".
5. Pulsar "Anterior" → seleccionar opción → "Siguiente" → ambas se guardan correctamente.

**Causa raíz:**
> El bug ocurre porque `AppUserQuestionnaireViewModel` (línea 331) llama `_ = InitializeAsync()`
> en el constructor sin parámetros, y el setter de `ResumeResponseId` (línea 266) también lo llama
> tras resetear el guard. Ambas instancias del método ejecutan `GoToPreviousQuestion` concurrentemente,
> dejando el servidor 2 posiciones atrás del estado que el cliente muestra.

**Categoría:** B (race condition async) + H (comportamiento silencioso: el segundo init no lanza
excepción, solo deja el estado corrompido).

**Checklist de verificación post-fix:**
1. ✅ Pulsar "Editar cuestionario" → la pregunta mostrada es la ÚLTIMA del cuestionario (no la penúltima).
2. ✅ Responder esa pregunta y pulsar "Siguiente" → cuestionario se completa → navega a `QuestionnaireSummaryView`.
3. ✅ Flujo normal de onboarding (sin `ResumeResponseId`) → pregunta 1, comportamiento idéntico al anterior.
4. ✅ Pulsar "Anterior" desde la última pregunta → muestra la penúltima pregunta (una posición atrás).
5. ✅ `Debug.WriteLine` en `InitializeAsync` muestra exactamente UNA llamada a `GoToPreviousQuestion` por navegación.
6. ✅ El `ProgressText` muestra la posición correcta de la pregunta tras la reanudación.

---

### `[AGENTE: arch-review]` — Fix del ViewModel

**Archivo:** `ViewModels/AppUserQuestionnaireViewModel.cs`

**Estrategia: `CancellationTokenSource`**

Cuando el setter de `ResumeResponseId` dispara, cancela el `InitializeAsync` que arrancó
desde el constructor. La primera llamada aborta limpiamente al chequear el token tras su primer
`await`. La segunda (modo reanudación) corre sin interferencia.

---

**Cambio 1 — Añadir `_initCts` en la región de Fields:**

```csharp
// Estado de la sesión
private long _responseId;
private long _resumeResponseId;
private bool _initializationStarted;
private CancellationTokenSource _initCts = new();   // ← añadir
private bool _isLoading;
```

---

**Cambio 2 — Setter de `ResumeResponseId`:** cancelar el init anterior antes de relanzar:

```csharp
public long ResumeResponseId
{
    get => _resumeResponseId;
    set
    {
        _resumeResponseId = value;
        _initCts.Cancel();                  // aborta la primera InitializeAsync
        _initCts = new CancellationTokenSource();
        _initializationStarted = false;
        _ = InitializeAsync(_initCts.Token);
    }
}
```

---

**Cambio 3 — Constructor sin parámetros:** pasar el token desde el arranque:

```csharp
public AppUserQuestionnaireViewModel() : this(
    App.GetService<QuestionnaireService>() ?? throw new InvalidOperationException("QuestionnaireService no registrado"),
    Preferences.Get("UserId", 0L),
    Preferences.Get("CoachId", 0L),
    Preferences.Get("ExperienceLevelId", 0L))
{
    _ = InitializeAsync(_initCts.Token);   // ← pasar el token del campo
}
```

---

**Cambio 4 — `InitializeAsync`: firma + check de cancelación + fix de `CurrentQuestionIndex`:**

```csharp
private async Task InitializeAsync(CancellationToken ct = default)
{
    if (_initializationStarted) return;
    _initializationStarted = true;

    try
    {
        IsLoading = true;
        StatusMessage = "Personalizando cuestionario...";

        Debug.WriteLine($"InitializeAsync — userId={_userId} coachId={_coachId} experienceId={_experienceLevelId} resumeId={_resumeResponseId}");

        if (_userId <= 0)
            throw new InvalidOperationException("UserId no válido.");
        if (_coachId <= 0)
            throw new InvalidOperationException("CoachId no válido.");
        if (_experienceLevelId <= 0)
            throw new InvalidOperationException("ExperienceLevelId no válido.");

        var questionnaireDto = await _questionnaireService
            .GetQuestionnaireByCoachIdAndExperienceLevelId(_coachId, _experienceLevelId);

        ct.ThrowIfCancellationRequested();   // ← punto de cancelación 1

        if (questionnaireDto == null)
            throw new InvalidOperationException("No se encontró cuestionario.");

        _questionnaireId = questionnaireDto.Id;
        if (_questionnaireId <= 0)
            throw new InvalidOperationException("QuestionnaireId no válido.");

        TotalQuestions = questionnaireDto.Questions?.Count ?? 0;

        QuestionnaireResponseDTO? response;

        if (_resumeResponseId > 0)
        {
            StatusMessage = "Cargando última pregunta...";
            _responseId = _resumeResponseId;

            response = await _questionnaireService.GoToPreviousQuestion(_responseId);

            ct.ThrowIfCancellationRequested();   // ← punto de cancelación 2

            if (response == null)
            {
                Debug.WriteLine("Sesión completada sin retroceso disponible, iniciando nueva sesión.");
                StatusMessage = "Iniciando nuevo cuestionario...";
                response = await _questionnaireService.StartQuestionnaire(_userId, _questionnaireId);
                ct.ThrowIfCancellationRequested();
                if (response != null) _responseId = response.ResponseId;
            }
        }
        else
        {
            response = await _questionnaireService.StartQuestionnaire(_userId, _questionnaireId);
            ct.ThrowIfCancellationRequested();   // ← punto de cancelación 3
            if (response != null) _responseId = response.ResponseId;
        }

        if (response == null)
        {
            await NotificationService.ShowErrorAsync("No se pudo iniciar el cuestionario. Por favor, intenta nuevamente.");
            return;
        }

        Debug.WriteLine($"Sesión lista — responseId={_responseId} isCompleted={response.IsCompleted} totalAnswered={response.TotalQuestionsAnswered}");

        if (response.CurrentQuestion != null)
        {
            CurrentQuestion = response.CurrentQuestion;
            // TotalQuestionsAnswered es el índice 0-based de la pregunta actual:
            // consistente con GoBackAsync que también usa TotalQuestionsAnswered directamente.
            CurrentQuestionIndex = response.TotalQuestionsAnswered;

            Debug.WriteLine($"Pregunta cargada: index={CurrentQuestionIndex} texto={CurrentQuestion.Text}");
        }
        else
        {
            await NotificationService.ShowErrorAsync("El cuestionario no tiene preguntas disponibles.");
        }
    }
    catch (OperationCanceledException)
    {
        // Cancelación limpia: el setter de ResumeResponseId lanzó un nuevo InitializeAsync.
        // No mostrar error al usuario.
        Debug.WriteLine("InitializeAsync cancelado — sobreescrito por modo reanudación.");
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error en InitializeAsync: {ex.Message}");
        Debug.WriteLine($"StackTrace: {ex.StackTrace}");
        await NotificationService.ShowErrorAsync("No se pudo cargar el cuestionario. Por favor, intenta nuevamente.");
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
- `GoBackAsync` sigue usando `response.TotalQuestionsAnswered` directamente (ya es correcto, no cambiar).
- `OnQuestionnaireCompleted` sigue haciendo `Preferences.Set("responseId", _responseId)`.
- El constructor DI con parámetros no cambia.
- `RestartQuestionnaireAsync` llama a `InitializeAsync()` sin token — correcto, sigue siendo
  una llamada explícita del usuario, no un init automático conflictivo.

---

### `[AGENTE: ui-review]` — Auditoría: sin código-behind que interfiera

**Archivo:** `Views/AppUserQuestionnaireView.xaml` y `Views/AppUserQuestionnaireView.xaml.cs`

**Tarea:** Confirmar que la View NO tiene un `OnAppearing` override ni un `EventToCommandBehavior`
vinculado a `Appearing` que pudiera disparar una tercera inicialización del ViewModel,
ya que el fix del arch-review asume que el único disparo de `InitializeAsync` es desde
el constructor y desde el setter de `ResumeResponseId`.

**Verificar:**
- [ ] `AppUserQuestionnaireView.xaml.cs` no tiene `override void OnAppearing()` ni `override void OnNavigatedTo()`.
- [ ] `AppUserQuestionnaireView.xaml` no tiene `<ContentPage.Behaviors>` con `EventToCommandBehavior EventName="Appearing"`.
- [ ] No hay ninguna llamada a `InitializeAsync` o `RestartQuestionnaireAsync` desde la View.

Si existiera alguno de estos, reportarlo sin aplicar cambios — requiere coordinación con arch-review.

---

## Nota sobre el backend

Verificar que `POST /questionnaires/responses/{responseId}/previous` sobre una sesión `COMPLETED`:
1. Cambia `IsCompleted` de `true` a `false` en la entidad de la sesión.
2. Elimina la última respuesta registrada.
3. Devuelve `QuestionnaireResponseDTO` con `IsCompleted = false` y la pregunta re-abierta.

Si el servidor no cambia `IsCompleted = false`, `AnswerQuestion` seguirá fallando incluso
con el fix del cliente (la sesión completada rechazaría nuevas respuestas).
Esto requeriría un fix adicional en el backend independiente del cliente.

---

## Resumen de archivos afectados

| Archivo | Agente | Cambio |
|---|---|---|
| `ViewModels/AppUserQuestionnaireViewModel.cs` | arch-review | Campo `_initCts` + CancellationToken en `InitializeAsync` + fix `CurrentQuestionIndex` |
| `Views/AppUserQuestionnaireView.xaml(.cs)` | ui-review | Solo auditoría, sin cambios XAML |
| — | bug-report | Diagnóstico formal + checklist de verificación |
| Backend endpoint `/previous` | — | Verificar reset de `IsCompleted` (fuera de scope cliente) |
