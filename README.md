# iFit App — Cliente móvil multiplataforma

**Aplicación .NET MAUI para la gestión de rutinas de entrenamiento con inteligencia artificial**

![.NET MAUI](https://img.shields.io/badge/.NET_MAUI-9.0-512BD4?style=flat-square&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-12-239120?style=flat-square&logo=csharp)
![MVVM](https://img.shields.io/badge/Patrón-MVVM-blueviolet?style=flat-square)
![CommunityToolkit](https://img.shields.io/badge/CommunityToolkit.Mvvm-✓-0078D4?style=flat-square)
![SQLite](https://img.shields.io/badge/SQLite-local-003B57?style=flat-square&logo=sqlite)

</div>

---

## Tabla de contenidos

- [Descripción general](#descripción-general)
- [Pantallas y flujo de la aplicación](#pantallas-y-flujo-de-la-aplicación)
- [Arquitectura MVVM](#arquitectura-mvvm)
- [Servicios](#servicios)
- [Almacenamiento local](#almacenamiento-local)
- [Tecnologías](#tecnologías)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Instalación y requisitos](#instalación-y-requisitos)
- [Configuración de la API](#configuración-de-la-api)

---

## Descripción general

iFit App es el cliente frontend del ecosistema iFit, desarrollado con **.NET MAUI** para ejecutarse de forma nativa en Android, iOS, Windows y macOS desde una única base de código en C#.

La aplicación implementa el patrón **MVVM** (Model-View-ViewModel) apoyado en `CommunityToolkit.Mvvm`, mantiene una base de datos local **SQLite** para el perfil del usuario y el historial de conversaciones con los coaches, y se comunica con la API backend a través de un servicio HTTP centralizado con gestión automática de tokens JWT.

---

## Pantallas y flujo de la aplicación

### Onboarding y autenticación

El usuario accede por primera vez a la pantalla de bienvenida (`GetStartedView`) y puede registrarse o iniciar sesión. El registro incluye validación de email con un código de verificación antes de acceder a la aplicación.

| Pantalla | ViewModel | Descripción |
|---|---|---|
| `GetStartedView` | `GetStartedViewModel` | Pantalla de bienvenida con opciones de acceso |
| `SignInView` | `SignInViewModel` | Login con email y contraseña |
| `SignUpView` | `SignUpViewModel` | Registro de nuevo usuario |
| `VerificationView` | `VerificationViewModel` | Verificación del email con código |

### Configuración inicial del perfil

Una vez autenticado, el usuario configura su perfil de entrenamiento eligiendo su nivel de experiencia y el modelo de coach de IA que quiere usar. Estos datos se envían al backend y condicionan la personalización de la experiencia.

| Pantalla | ViewModel | Descripción |
|---|---|---|
| `ExperienceLevelSelectionView` | `ExperienceLevelViewModel` | Selección del nivel de experiencia (principiante, intermedio, avanzado) |
| `CoachModelTypeSelectionView` | `CoachModelTypeSelectionViewModel` | Elección del coach de IA (Ronnie, Serena, Kael, Eliud, Master) |

### Home y navegación principal

La pantalla principal (`HomeView`) actúa como hub de la aplicación. Muestra un carrusel informativo y el acceso rápido a la rutina activa del usuario. La barra de navegación inferior divide la app en cinco secciones: **Hoy**, **Plan**, **Actividades**, **Comunidad** y **Ayuda**.

### Cuestionario adaptativo

Flujo central de la generación de rutinas con IA. El `AppUserQuestionnaireViewModel` gestiona la sesión pregunta a pregunta: carga la primera pregunta desde el backend, procesa cada respuesta, determina la siguiente pregunta según la rama del árbol de decisiones, muestra el progreso al usuario y, al completar el cuestionario, almacena el `responseId` para la posterior generación de la rutina.

| Pantalla | ViewModel | Descripción |
|---|---|---|
| `AppUserQuestionnaireView` | `AppUserQuestionnaireViewModel` | Motor del cuestionario paso a paso |
| `QuestionnaireSummaryView` | `QuetionnaireSummaryViewModel` | Resumen de las respuestas antes de generar la rutina |

### Generación y visualización de rutinas

Una vez completado el cuestionario, el usuario puede generar su rutina con IA o crearla manualmente. El `AIGenerationRoutineViewModel` orquesta la llamada al backend (que a su vez llama al microservicio Ronnie), gestiona el estado de carga durante la generación (que puede tardar varios segundos) y navega al resultado.

| Pantalla | ViewModel | Descripción |
|---|---|---|
| `AIGenerationRoutineView` | `AIGenerationRoutineViewModel` | Pantalla de generación con IA y estado de espera |
| `ManualRoutineBuilderView` | `ManualRoutineBuilderViewModel` | Constructor manual de rutina por días y ejercicios |
| `RoutineSummaryView` | `RoutineSummaryViewModel` | Resumen de la rutina generada o creada |

### Seguimiento del plan de entrenamiento

El módulo de plan permite al usuario ver su rutina activa organizada por días, acceder al detalle de cada sesión, consultar los ejercicios asignados y registrar el progreso semanal.

| Pantalla | ViewModel | Descripción |
|---|---|---|
| `PlanView` | `PlanViewModel` | Vista general del plan activo con días de entrenamiento |
| `PlanSummaryView` | `PlanSummaryViewModel` | Resumen completo de la rutina con todos los días |
| `TrainingDayDetailView` | `TrainingDayDetailViewModel` | Detalle de una sesión: ejercicios, series, repeticiones y descanso |
| `WeeklySummaryView` | `WeeklySummaryViewModel` | Resumen semanal de actividad |

### Catálogo de ejercicios

Listado paginado y buscable de todos los ejercicios disponibles. El usuario puede explorar el catálogo, ver el detalle de cada ejercicio y usarlo como referencia durante el entrenamiento.

| Pantalla | ViewModel | Descripción |
|---|---|---|
| `ExerciseCatalogView` | `ExerciseCatalogViewModel` | Listado paginado con búsqueda en tiempo real |
| `ExerciseDetailView` | `ExerciseDetailViewModel` | Ficha detallada del ejercicio |

### Chat con el coach de IA

Permite al usuario mantener conversaciones con su entrenador virtual. El `ChatAIViewModel` gestiona el historial de mensajes de la sesión actual y se comunica con el microservicio Ronnie a través del backend. El `memoryId` de la conversación se persiste en SQLite para mantener el contexto entre sesiones.

| Pantalla | ViewModel | Descripción |
|---|---|---|
| `ChatAIView` | `ChatAIViewModel` | Interfaz de chat con el coach seleccionado |

### Perfil de usuario

| Pantalla | ViewModel | Descripción |
|---|---|---|
| `ProfileView` | `ProfileViewModel` | Gestión del perfil, datos personales y cierre de sesión |

---

## Arquitectura MVVM

La aplicación sigue estrictamente el patrón **MVVM** usando `CommunityToolkit.Mvvm`:

- **Models** — Clases de dominio y DTOs que mapean las respuestas de la API. Incluyen entidades como `AppUser`, `CoachConversation`, `TokenDTO` y todos los DTOs de autenticación, cuestionario, rutinas y ejercicios.

- **Views** — Páginas XAML que definen la interfaz de usuario. No contienen lógica de negocio: se limitan a enlazar propiedades y comandos del ViewModel mediante `{Binding}`. Los controles de usuario reutilizables (`HomeHeader`, `HomeFooter`, `LoadingOverlay`, `ToastNotification`, `PlanDayDetailView`) viven en `Views/Components/`.

- **ViewModels** — Heredan de `ObservableObject` y exponen propiedades reactivas con `[ObservableProperty]` y comandos con `[RelayCommand]`. Gestionan el estado de la UI, orquestan llamadas a los servicios y realizan la navegación mediante `Shell.Current.GoToAsync`.

La comunicación entre capas usa el sistema de **data binding** de MAUI con notificación automática de cambios generada por el source generator de `CommunityToolkit.Mvvm`, sin necesidad de implementar `INotifyPropertyChanged` manualmente.

---

## Servicios

Todos los servicios viven en la capa `Services/` y son inyectados en los ViewModels.

### `WebService`

Servicio HTTP centralizado que encapsula todas las comunicaciones con la API backend. Gestiona de forma automática:

- La adición del header `Authorization: Bearer <token>` en todas las peticiones autenticadas.
- La renovación del `access_token` mediante `refresh_token` cuando el servidor devuelve un 401, sin interrumpir la petición original.
- La serialización y deserialización JSON con opciones reutilizables (`PropertyNameCaseInsensitive`).
- Un `HttpClient` dedicado para las peticiones al servicio de IA con un timeout extendido de 1800 segundos, necesario para la generación de rutinas con LLM.

### `AuthenticationService`

Gestiona el ciclo de vida de la sesión del usuario: login, registro, verificación de email, refresco de tokens y logout. Después de un login o registro exitoso, almacena los tokens en `SecureStorage` a través del `WebService`.

### `AIRoutineService`

Orquesta la generación de rutinas con IA y el chat con los coaches. Gestiona los `memoryId` de cada conversación: los obtiene del backend la primera vez y los persiste en SQLite para recuperar el contexto en sesiones futuras.

### `QuestionnaireService`

Gestiona todo el ciclo del cuestionario adaptativo: iniciar una sesión de respuestas, obtener la siguiente pregunta según la respuesta anterior, enviar respuestas y obtener el resumen final con el `responseId` necesario para la generación de la rutina.

### `TrainingService`

Gestiona las rutinas de entrenamiento: obtener la rutina activa del usuario, listar los días de entrenamiento y sus ejercicios, y el seguimiento del progreso.

### `AppUserService`

CRUD del perfil de usuario: obtener datos, actualizar información personal y cambiar configuraciones de la cuenta.

### `ExerciseCatalogService`

Consulta paginada del catálogo de ejercicios con soporte de búsqueda por nombre.

### `CoachModelTypeService` / `ExperienceLevelService`

Obtienen los modelos de coach y niveles de experiencia disponibles desde el backend para mostrarlos en la selección inicial.

### `DatabaseService`

Gestiona la base de datos SQLite local. Inicializa las tablas necesarias (`AppUser`, `CoachModelTypeDto`) y proporciona operaciones de lectura/escritura del usuario actual, usando `Preferences` de MAUI para almacenar el `UserId` activo.

### `NotificationService`

Servicio estático para mostrar notificaciones toast animadas (éxito, error, información) sobre la página activa. Se inyecta sobre el `Grid` raíz de la `ContentPage` actual con una animación de slide-up y fade, sin interrumpir la navegación.

### `ISecureStorageService`

Abstracción sobre `SecureStorage` de MAUI para almacenar de forma segura los tokens JWT. La interfaz permite sustituir la implementación en pruebas unitarias sin dependencia del sistema operativo.

---

## Almacenamiento local

La aplicación combina tres mecanismos de persistencia local:

**SQLite** (vía `sqlite-net-pcl`) almacena el perfil del usuario (`AppUser`) y el historial de conversaciones con los coaches (`CoachConversation`), incluyendo los `memoryId` que mantienen el contexto de cada conversación con el LLM entre sesiones.

**`SecureStorage` de MAUI** almacena los tokens JWT (`access_token` y `refresh_token`) de forma cifrada usando el keychain del sistema operativo (Keychain en iOS/macOS, Keystore en Android, DPAPI en Windows).

**`Preferences` de MAUI** almacena valores ligeros como el `UserId` del usuario activo para acceso rápido sin consultar la base de datos.

---

## Tecnologías

| Capa | Tecnología |
|---|---|
| Framework UI | .NET MAUI 9 |
| Lenguaje | C# 12 |
| Patrón de presentación | MVVM con CommunityToolkit.Mvvm |
| Base de datos local | SQLite (sqlite-net-pcl) |
| Almacenamiento seguro | MAUI SecureStorage |
| Comunicación HTTP | HttpClient (System.Net.Http) |
| Serialización | System.Text.Json |
| Componentes UI | .NET MAUI Community Toolkit |
| Plataformas objetivo | Android, iOS, Windows, macOS |

---

## Estructura del proyecto

```
IFit/
├── Models/                         # Clases de dominio y DTOs
│   ├── AppUser.cs
│   ├── CoachConversation.cs        # Entidad SQLite para historial de chat
│   ├── TokenDTO.cs
│   └── Dtos/
│       ├── Auth/                   # DTOs de login, registro, tokens
│       ├── AppUser/                # DTOs de perfil de usuario
│       ├── Questionnaire/          # DTOs del cuestionario adaptativo
│       ├── Routine/                # DTOs de rutinas y días de entrenamiento
│       ├── Exercise/               # DTOs del catálogo de ejercicios
│       ├── Ronnie/                 # DTOs del chat con coaches IA
│       ├── CoachModelType/         # DTOs de modelos de coach
│       └── ExperienceLevel/        # DTOs de nivel de experiencia
│
├── Services/                       # Lógica de negocio y acceso a datos
│   ├── WebService.cs               # Cliente HTTP centralizado con gestión JWT
│   ├── AuthenticationService.cs    # Login, registro, tokens
│   ├── AIRoutineService.cs         # Generación IA y chat con coaches
│   ├── QuestionnaireService.cs     # Motor del cuestionario adaptativo
│   ├── TrainingService.cs          # Rutinas y seguimiento
│   ├── AppUserService.cs           # Perfil de usuario
│   ├── ExerciseCatalogService.cs   # Catálogo de ejercicios paginado
│   ├── CoachModelTypeService.cs    # Tipos de coach
│   ├── ExperienceLevelService.cs   # Niveles de experiencia
│   ├── DatabaseService.cs          # SQLite local
│   ├── NotificationService.cs      # Toasts animados
│   └── ISecureStorageService.cs    # Abstracción de SecureStorage
│
├── ViewModels/                     # Lógica de presentación (MVVM)
│   ├── SignInViewModel.cs
│   ├── SignUpViewModel.cs
│   ├── VerificationViewModel.cs
│   ├── GetStartedViewModel.cs
│   ├── ExperienceLevelViewModel.cs
│   ├── CoachModelTypeSelectionViewModel.cs
│   ├── HomeViewModel.cs
│   ├── AppUserQuestionnaireViewModel.cs
│   ├── AIGenerationRoutineViewModel.cs
│   ├── ManualRoutineBuilderViewModel.cs
│   ├── RoutineSummaryViewModel.cs
│   ├── PlanViewModel.cs
│   ├── PlanSummaryViewModel.cs
│   ├── TrainingDayDetailViewModel.cs
│   ├── WeeklySummaryViewModel.cs
│   ├── ExerciseCatalogViewModel.cs
│   ├── ExerciseDetailViewModel.cs
│   ├── ChatAIViewModel.cs
│   ├── ProfileViewModel.cs
│   └── Components/                 # ViewModels de componentes reutilizables
│
└── Views/                          # Páginas XAML
    ├── SignInView.xaml
    ├── SignUpView.xaml
    ├── VerificationView.xaml
    ├── GetStartedView.xaml
    ├── ExperienceLevelSelectionView.xaml
    ├── CoachModelTypeSelectionView.xaml
    ├── HomeView.xaml
    ├── AppUserQuestionnaireView.xaml
    ├── AIGenerationRoutineView.xaml
    ├── ManualRoutineBuilderView.xaml
    ├── RoutineSummaryView.xaml
    ├── PlanView.xaml
    ├── PlanSummaryView.xaml
    ├── TrainingDayDetailView.xaml
    ├── WeeklySummaryView.xaml
    ├── ExerciseCatalogView.xaml
    ├── ExerciseDetailView.xaml
    ├── ChatAIView.xaml
    ├── ProfileView.xaml
    └── Components/                 # Controles de usuario reutilizables
        ├── HomeHeader.xaml
        ├── HomeFooterView.xaml     # Barra de navegación inferior
        ├── LoadingOverlay.xaml
        ├── ToastNotification.xaml
        └── PlanDayDetailView.xaml
```

---

## Instalación y requisitos

### Requisitos previos

- Visual Studio 2022 (v17.8 o superior) con la carga de trabajo **.NET MAUI** instalada, o Rider con el plugin MAUI.
- .NET 9 SDK.
- Para despliegue en Android: Android SDK (API 21 o superior). Para iOS/macOS: Xcode 15+.
- El backend de iFit levantado y accesible (ver README del proyecto API).

### Pasos de instalación

```bash
# 1. Clonar el repositorio
git clone <url-del-repositorio>
cd IFitApp

# 2. Restaurar paquetes NuGet
dotnet restore

# 3. Configurar la URL de la API (ver sección siguiente)

# 4. Ejecutar en el emulador o dispositivo
dotnet build -t:Run -f net9.0-android
# o en iOS:
dotnet build -t:Run -f net9.0-ios
```

---

## Configuración de la API

La URL base de la API se configura en el archivo de ajustes de la aplicación (`AppSettings`). Por defecto apunta al API Gateway en local:

```
http://localhost:8080/ifit/api/v1
```

Para apuntar a un entorno de desarrollo o producción distinto, basta con cambiar esta URL. El `WebService` gestiona automáticamente la autenticación y el refresco de tokens independientemente del entorno al que apunte.

Si se ejecuta en un emulador Android, reemplazar `localhost` por `10.0.2.2` (que es la IP del host desde el emulador de Android):

```
http://10.0.2.2:8080/ifit/api/v1
```
