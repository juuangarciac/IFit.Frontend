# Request: Bug — ButtonContent con nombre de coach del usuario anterior en HomeView

## Descripción del bug
El botón "Pregunta a [Coach]" en `HomeView` siempre muestra el nombre del coach
del último usuario que inició sesión, en lugar del coach del usuario actual.

## Análisis previo

### Causa raíz — Categoría F (ShellContent cacheada sin reinicialización entre sesiones)

`HomeView` está registrada como **ShellContent** en `AppShell.xaml` → su ViewModel
`HomeViewModel` se crea una sola vez y persiste durante toda la sesión de la app.

`HomeViewModel` tiene el patrón `_isInitialized` correcto, pero el flag **nunca se resetea**
cuando un nuevo usuario inicia sesión:

```csharp
// HomeViewModel.cs — AppearingAsync (línea 139)
[RelayCommand]
public Task AppearingAsync()
{
    if (_isInitialized) return Task.CompletedTask;  // ← el flag ya es true del usuario anterior
    _isInitialized = true;
    IsLoading = true;
    _ = InitializeAsync();
    return Task.CompletedTask;
}
```

Cuando el usuario 2 inicia sesión:
1. `SignInViewModel.SaveLoginData()` escribe el nuevo `CoachModelTypeName` en Preferences ✅
2. `GoToAsync("///HomeView")` navega a HomeView y dispara `Appearing`
3. `AppearingAsync` → `_isInitialized = true` (del usuario anterior) → **retorna sin re-inicializar**
4. `InitializeAsync` nunca corre → `ButtonContent` sigue siendo `"Pregunta a [Coach del usuario 1]"`

El dato de `CoachModelTypeName` se lee **solo dentro de `InitializeAsync`** (línea 98),
que está protegida por el guard. Resultado: datos de rutina y nombre de coach son del usuario anterior.

### Archivos afectados

| Archivo | Problema |
|---|---|
| `ViewModels/HomeViewModel.cs:74` | `_isInitialized` nunca se resetea entre sesiones |
| `ViewModels/HomeViewModel.cs:98` | `CoachModelTypeName` solo se lee en `InitializeAsync` |

### Preferences involucradas

| Clave | Escritor | Lector afectado |
|---|---|---|
| `CoachModelTypeName` | `SignInViewModel.SaveLoginData` | `HomeViewModel.InitializeAsync` |
| `UserId` | `VerificationViewModel` / `SignInViewModel` | `HomeViewModel.InitializeAsync` |

---

## Flujo de trabajo por agentes

---

### `[AGENTE: bug-report]` — Diagnóstico formal y verificación

**Precondiciones:**
- Dispositivo con dos cuentas creadas, cada una con un coach distinto (ej. usuario A: "Ronnie", usuario B: "Arnold").

**Pasos para reproducir:**
1. Iniciar sesión con usuario A (coach "Ronnie") → llegar a `HomeView`.
2. Verificar que el botón muestra `"Pregunta a Ronnie"`. ✅
3. Cerrar sesión / volver a la pantalla de login.
4. Iniciar sesión con usuario B (coach "Arnold") → llegar a `HomeView`.
5. Observar el botón inferior.

**Resultado observado:** `"Pregunta a Ronnie"` (coach del usuario A).

**Resultado esperado:** `"Pregunta a Arnold"` (coach del usuario B).

**Causa raíz:**
> El bug ocurre porque `HomeViewModel._isInitialized` (línea 74) nunca se resetea a `false`
> entre sesiones de usuario, por lo que `AppearingAsync` (línea 141) retorna sin ejecutar
> `InitializeAsync`, donde se lee `CoachModelTypeName` de Preferences.

**Categoría:** F — ShellContent cacheada que no se reinicializa.

**Impacto secundario:** Además del `ButtonContent`, `Routine` y `TrainingDayDto` también muestran
datos del usuario anterior. El bug afecta a todos los datos que carga `InitializeAsync`.

**Checklist de verificación post-fix:**
1. ✅ Usuario B inicia sesión → botón muestra coach de B, no de A.
2. ✅ Usuario B inicia sesión → la rutina mostrada es la de B, no la de A.
3. ✅ Usuario A inicia sesión de nuevo → datos vuelven a ser los de A.
4. ✅ Un único usuario que navega y vuelve a `HomeView` no re-dispara `InitializeAsync`
   (el guard sigue funcionando para evitar recargas innecesarias dentro de la misma sesión).
5. ✅ Si `UserId` en Preferences es 0 (sesión limpia), `InitializeAsync` se comporta normalmente.

---

### `[AGENTE: arch-review]` — Fix del ViewModel

**Archivo:** `ViewModels/HomeViewModel.cs`

**Estrategia:** Comparar el `UserId` actual de Preferences con el del último usuario
que inicializó el VM. Si son distintos, resetear el flag y recargar todos los datos.
Esto corrige el bug para `ButtonContent`, `Routine` y `TrainingDayDto` en un solo cambio.

**Cambio 1 — Añadir campo `_lastUserId` en la región de campos:**

```csharp
private bool _isInitialized = false;
private long _lastUserId = 0;        // ← añadir esta línea
```

**Cambio 2 — Sustituir `AppearingAsync` completo:**

```csharp
// ANTES:
[RelayCommand]
public Task AppearingAsync()
{
    if (_isInitialized) return Task.CompletedTask;
    _isInitialized = true;
    IsLoading = true;
    _ = InitializeAsync();
    return Task.CompletedTask;
}

// DESPUÉS:
[RelayCommand]
public Task AppearingAsync()
{
    long currentUserId = Preferences.Get("UserId", 0L);

    // Re-inicializar si es la primera vez o si cambió el usuario activo
    if (_isInitialized && currentUserId == _lastUserId) return Task.CompletedTask;

    _isInitialized = true;
    _lastUserId = currentUserId;
    IsLoading = true;
    _ = InitializeAsync();
    return Task.CompletedTask;
}
```

**Por qué esta estrategia y no otras:**
- **No** mover `CoachModelTypeName` fuera del guard: el problema es más amplio
  (`Routine` y `TrainingDayDto` también son del usuario anterior).
- **No** resetear `_isInitialized` desde `SignInViewModel`: crea acoplamiento entre VMs,
  viola la separación de responsabilidades.
- **No** eliminar el guard por completo: `HomeView` es ShellContent y vuelve a aparecer
  en cada navegación de vuelta (ej. desde `ProfileView`). Sin guard, `InitializeAsync`
  dispararía llamadas HTTP en cada `GoToAsync("..")`.

**Restricciones — no romper:**
- El constructor de `HomeViewModel` no cambia.
- `InitializeAsync` no cambia internamente (la lógica de lectura de `CoachModelTypeName`
  y de la rutina queda intacta).
- El `AppearingAsync` sigue siendo fire-and-forget (`_ = InitializeAsync()`).

---

### `[AGENTE: ui-review]` — Auditoría de bindings afectados en HomeView.xaml

**Archivo:** `Views/HomeView.xaml`

**Tarea:** No hay cambios de diseño necesarios. Auditar que todos los elementos que muestran
datos del usuario estén correctamente vinculados a propiedades observables del ViewModel,
para confirmar que el fix del VM es suficiente y que no hay valores hardcodeados que
puedan enmascarar el bug o sobrevivir a la corrección.

**Bindings a verificar:**

| Elemento XAML (línea aprox.) | Binding | ¿Reactivo al fix? |
|---|---|---|
| `Button` (línea 383) | `Text="{Binding ButtonContent}"` | ✅ `ButtonContent` es `[ObservableProperty]` |
| `Label` (línea 211) | `Text="{Binding TrainingDayDto.DayName}"` | ✅ `TrainingDayDto` es `[ObservableProperty]` |
| `Label` (línea 222) | `Text="{Binding TrainingDayDto.Description}"` | ✅ |
| `Span` (línea 311) | `Text="{Binding TrainingDayDto.DayNumber}"` | ✅ |
| `Span` (línea 315) | `Text="{Binding Routine.TrainingDays}"` | ✅ `Routine` es `[ObservableProperty]` |

**Comprobaciones adicionales:**
- [ ] El `EventToCommandBehavior` de `Appearing` (línea 12) tiene `Command="{Binding AppearingCommand}"` — confirmar que el nombre del command generado por CommunityToolkit coincide exactamente (`AppearingCommand`, no `AppearingAsyncCommand`).
- [ ] El `IsVisible` del botón inferior (línea 389) está vinculado a `IsLoading` con `InvertedBoolConverter` — verificar que `IsLoading` se pone en `true` al inicio de la reinicialización y en `false` en el `finally` de `InitializeAsync`.
- [ ] No hay ningún `Label` o `Button` con el nombre del coach escrito como texto estático (`Text="Pregunta a Ronnie"`).

---

## Resumen de archivos afectados

| Archivo | Agente | Cambio |
|---|---|---|
| `ViewModels/HomeViewModel.cs` | arch-review | Campo `_lastUserId` + lógica de reset en `AppearingAsync` |
| `Views/HomeView.xaml` | ui-review | Solo auditoría de bindings, sin cambios XAML |
| — | bug-report | Diagnóstico formal + checklist de verificación |
