# IFit MAUI — Contexto del proyecto para Claude

## Visión general
App móvil Android (TFG) en **.NET MAUI 9.0** que funciona como asistente de entrenamiento personalizado con IA.
El objetivo del TFG es demostrar una arquitectura MVVM limpia sobre MAUI con backend REST propio.

---

## Stack tecnológico
| Capa | Tecnología |
|---|---|
| Framework | .NET MAUI 9.0 |
| MVVM | CommunityToolkit.Mvvm 8.x (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`) |
| Base de datos local | SQLite-net-pcl (`SQLiteAsyncConnection`) |
| Almacenamiento seguro | `SecureStorage` + wrapper `ISecureStorageService` |
| Preferencias rápidas | `Microsoft.Maui.Storage.Preferences` |
| HTTP | `HttpClient` singleton en `AppSettings._HttpClient` |
| Validación formularios | Plugin.ValidationRules (`Validatable<T>`) |
| UI extras | CommunityToolkit.Maui |

---

## Configuración de red
- API base: `http://192.168.1.71:8080/ifit/api/v1` (definido en `AppSettings.cs`)
- Timeout HTTP normal: 5 s; timeout AI: 1800 s
- El fichero `Platforms/Android/Resources/xml/network_security_config.xml` permite tráfico HTTP en Android
- Refresh token endpoint: `/auth/refresh`

---

## Dependency Injection (`MauiProgram.cs`)
Todos los servicios están registrados como **Singleton**:
```
DatabaseService, AppUserService, AuthenticationService,
CoachModelTypeService, ExperienceLevelService, QuestionnaireService,
AIRoutineService, TrainingService, ExerciseCatalogService,
ISecureStorageService → SecureStorageService, WebService
```
`App.GetService<T>()` es el helper estático para resolver servicios desde código no-DI (p.ej. constructores sin parámetros de ViewModels).

---

## Navegación Shell — Regla fundamental

### ShellContent (caché — instancia única por sesión)
Registradas en `AppShell.xaml`. El ViewModel se crea **una sola vez** y se reutiliza.
Solo usar para páginas que NO deben reinicializarse entre usuarios/sesiones.

Páginas ShellContent actuales:
```
MainPage, SignUpView, SignInView, VerificationView, HomeView,
ErrorView, GetStartedView, RoutineSummaryView, PlanView, PlanSummaryView
```

### RegisterRoute (instancia nueva en cada push)
Registradas en `AppShell.xaml.cs`. Se crea una instancia nueva en cada navegación → el ViewModel lee Preferences frescos.
Usar para páginas del flujo de onboarding o cualquier página que dependa del usuario actual.

Páginas RegisterRoute actuales:
```
ExperienceLevelSelectionView, CoachModelTypeSelectionView,
AppUserQuestionnaireView, QuestionnaireSummaryView, AIGenerationRoutineView,
TrainingDayDetailView, ChatAIView, WeeklySummaryView, ProfileView,
ExerciseCatalogView, ExerciseDetailView, ManualRoutineBuilderView
```

### Prefijos de navegación
| Prefijo | Significado |
|---|---|
| `///ruta` | Reset completo del stack, navega a ShellContent |
| `//ruta` | Navegación Shell sin reset completo (ShellContent) |
| `"ruta"` | Push relativo al stack actual (RegisterRoute) |
| `".."` | Pop (volver atrás en el stack) |

**Regla crítica**: nunca usar `//` o `///` para navegar a un RegisterRoute; nunca usar un nombre simple para navegar a un ShellContent desde otro ShellContent cuando quieras reset de stack.

---

## Flujo de autenticación y registro

### Registro nuevo (`SignUpViewModel` → `VerificationViewModel` → `GetStartedViewModel` → ...)
```
SignUpView
  → (//VerificationView)  [ShellContent]
  → (//GetStartedView)    [ShellContent]
  → ("ExperienceLevelSelectionView")  [RegisterRoute push]
  → ("CoachModelTypeSelectionView")   [RegisterRoute push]
  → ("AppUserQuestionnaireView")      [RegisterRoute push]
  → (//QuestionnaireSummaryView)      [ShellContent]
  → (//AIGenerationRoutineView / //RoutineSummaryView)
  → (///HomeView)                     [ShellContent, reset]
```

### Login (`SignInViewModel`)
```
4a: No verificado → ///VerificationView
4b: Sin coach/experience → ///GetStartedView
4c: Tiene coach+experience pero RegistrationComplete=false → ///GetStartedView
     (el usuario re-selecciona nivel y coach, que setea Preferences correctos)
5:  Todo OK → ///HomeView
```

### RegistrationComplete
- El backend **no** marca automáticamente `RegistrationComplete = true`.
- Se marca en cliente llamando a `AppUserService.MarkRegistrationComplete(userId)` desde `RoutineSummaryViewModel.SaveRoutineAsync()` justo después de que el usuario guarda la rutina generada.

---

## Preferences — claves usadas
| Clave | Tipo | Dónde se escribe | Quién la lee |
|---|---|---|---|
| `UserId` | `long` | `VerificationViewModel` (síncrono, antes de navegar), `DatabaseService.InsertAppUserAsync` | Casi todo |
| `UserName` | `string` | `SignInViewModel.SaveLoginData`, `SignUpViewModel.SaveRegistrationData` | `GetStartedViewModel` |
| `UserEmail` | `string` | `SignInViewModel`, `SignUpViewModel` | - |
| `IsVerified` | `bool` | `SignInViewModel.HandleVerificationAsync` | - |
| `CoachId` | `long` | `CoachModelTypeSelectionViewModel` | `AppUserQuestionnaireViewModel` constructor |
| `CoachName` | `string` | `CoachModelTypeSelectionViewModel` | - |
| `CoachModelTypeName` | `string` | `SignInViewModel.SaveLoginData` | - |
| `ExperienceLevelId` | `long` | `ExperienceLevelViewModel` | `AppUserQuestionnaireViewModel` constructor |
| `ExperienceName` | `string` | `ExperienceLevelViewModel` | - |
| `responseId` | `long` | `AppUserQuestionnaireViewModel.OnQuestionnaireCompleted` | `QuestionnaireSummaryViewModel` |

**Importante**: `AppUserQuestionnaireViewModel` captura `UserId`, `CoachId` y `ExperienceLevelId` desde Preferences **en el constructor**. Como es RegisterRoute, esto es seguro porque se crea una instancia nueva en cada navegación con los Preferences actualizados del usuario actual.

---

## Base de datos local SQLite
- Tablas: `AppUser`, `CoachModelTypeDto`
- `DatabaseService.GetCurrentUserAsync()` lee `Preferences.Get("UserId", 0L)` y busca el usuario en la BD local.
- `InsertAppUserAsync`: hace INSERT si no existe, UPDATE si existe. Solo setea `Preferences.Set("UserId")` en el INSERT.
- `SaveAppUserAsync`: comportamiento similar.
- La BD local es **caché** — si falla, el flujo sigue porque el usuario ya está autenticado en el servidor.

---

## Patrón de ViewModel
```csharp
// Constructor DI (para tests o inyección explícita):
public MyViewModel(SomeService service) { ... }

// Constructor sin parámetros para XAML/MAUI Shell (llama al DI):
public MyViewModel() : this(App.GetService<SomeService>() ?? throw ...)
{
    _ = InitializeAsync(); // fire-and-forget solo si es ShellContent/init liviana
}
```

**Regla**: Si el ViewModel es de una página RegisterRoute, `InitializeAsync` puede ser fire-and-forget desde el constructor porque la página se crea justo antes de mostrarse.

---

## TokenManager
- Guarda `accessToken`, `refreshToken` y `expiry` en `SecureStorage`.
- Mantiene caché en memoria para evitar hits lentos al Keystore de Android.
- Claves SecureStorage: `ifit_access_token`, `ifit_refresh_token`, `ifit_token_expiry`, `ifit_user_data`.
- `WebService` usa `TokenManager` para adjuntar el Bearer token y refrescarlo automáticamente en 401.

---

## Estilo visual
- Tema oscuro permanente. Color principal de fondo: `#222831` (`BackgroundPrimaryDark`).
- Definido en `Resources/Styles/Colors.xaml` y `Resources/Styles/Styles.xaml`.
- El estilo base `Page` y `Shell` tienen `BackgroundColor = BackgroundPrimaryDark` para evitar flash blanco en Android.
- `Platforms/Android/Resources/values/styles.xml` sobreescribe `Maui.MainTheme` con `windowBackground = @color/colorPrimary` para el mismo motivo.

---

## Bugs corregidos en sesiones anteriores (no repetir)

### Flash blanco en navegación Android
- **Causa**: Color de fondo por defecto era blanco en la transición de fragmentos.
- **Fix**: `styles.xml` Android + `Styles.xaml` con `BackgroundPrimaryDark`.

### Race condition UserId en segundo registro
- **Causa**: `VerificationViewModel.VerifyEmailAsync` hacía `_ = InsertUserToDatabase(...)` (fire-and-forget) antes de navegar. `Preferences.UserId` no estaba actualizado cuando la siguiente pantalla lo leía.
- **Fix**: `Preferences.Set("UserId", authData.AppUser.Id)` síncrono + `await InsertUserToDatabase(...)`.

### Segundo registro hereda último cuestionario del primero
- **Causa**: `AppUserQuestionnaireView` era ShellContent → VM cacheada con `_responseId`, `_currentQuestionIndex`, `_userId` del usuario anterior.
- **Fix**: Movido a RegisterRoute. Cada registro crea una VM nueva con Preferences frescos.

### Preferences key mismatch "Name" vs "UserName"
- **Causa**: `SignInViewModel` guardaba con clave `"Name"`, `GetStartedViewModel` leía `"UserName"`.
- **Fix**: Unificado a `"UserName"` en todos los puntos de escritura.

### CoachModelTypeSelectionView enviaba ID de usuario anterior
- **Misma causa** que el bug de VerificationViewModel (race condition en UserId).

### RegistrationComplete nunca se marcaba
- **Causa**: `AppUserService.MarkRegistrationComplete` existía pero nunca se llamaba.
- **Fix**: Llamada añadida en `RoutineSummaryViewModel.SaveRoutineAsync` tras guardar la rutina.

---

## Estructura de directorios relevante
```
IFit/
├── AppSettings.cs          ← URLs de API, HttpClient, SQLite path
├── AppShell.xaml           ← Declara ShellContent (páginas cacheadas)
├── AppShell.xaml.cs        ← RegisterRoute (páginas instanciadas en cada push)
├── MauiProgram.cs          ← DI: todos los servicios como Singleton
├── App.xaml.cs             ← App.GetService<T>() helper
├── Models/
│   ├── AppUser.cs          ← Entidad SQLite local
│   ├── Dtos/
│   │   ├── Auth/AuthResponse.cs        ← accessToken, refreshToken, AppUser
│   │   ├── AppUser/AppUserResponse.cs  ← RegistrationComplete, CoachModelTypeName, etc.
│   │   └── Questionnaire/              ← DTOs cuestionario
├── Services/
│   ├── WebService.cs           ← HTTP base con auto-refresh JWT
│   ├── AuthenticationService.cs
│   ├── AppUserService.cs       ← SetCoachModelType, SetExperienceLevel, MarkRegistrationComplete
│   ├── DatabaseService.cs      ← SQLite local (caché de AppUser)
│   ├── QuestionnaireService.cs
│   ├── TrainingService.cs
│   └── AIRoutineService.cs
├── ViewModels/             ← Un ViewModel por vista
├── Views/                  ← Un .xaml + .xaml.cs por vista
├── Utils/TokenManager.cs
└── Platforms/Android/
    ├── Resources/xml/network_security_config.xml  ← Permite HTTP
    └── Resources/values/styles.xml                ← Fix flash blanco
```

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
