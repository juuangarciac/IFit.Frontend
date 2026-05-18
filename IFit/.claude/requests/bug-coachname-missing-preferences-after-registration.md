# Request: Bug — "Pregunta a tu coach" tras registro nuevo (CoachModelTypeName ausente en Preferences)

## Descripción del bug

Después de completar el flujo de registro (onboarding), `HomeView` muestra el botón
"Pregunta a tu coach" en lugar de "Pregunta a {nombre del coach elegido}".
Tras cerrar sesión e iniciar sesión con el mismo usuario, el botón sí muestra el nombre correcto.

---

## Causa raíz — Mismatch de clave en Preferences

### Lo que escribe `CoachModelTypeSelectionViewModel.HandleOnSelectedCoachChanged` (líneas 111-112):
```csharp
Preferences.Set("CoachId",   selectedCoachModelType.Id);
Preferences.Set("CoachName", selectedCoachModelType.Name);   // ← escribe "CoachName"
```

### Lo que lee `HomeViewModel.InitializeAsync`:
```csharp
var coachName = Preferences.Get("CoachModelTypeName", "tu coach");  // ← lee "CoachModelTypeName"
ButtonContent = "Pregunta a " + coachName;
```

**`"CoachModelTypeName"` nunca se escribe durante el onboarding** → `Preferences.Get` devuelve
el valor por defecto `"tu coach"` → el botón muestra "Pregunta a tu coach".

### Por qué funciona tras el login

`SignInViewModel.SaveLoginData` escribe AMBAS claves:
```csharp
Preferences.Set("CoachModelTypeName", dto.CoachModelTypeName);  // ← HomeViewModel la lee aquí
Preferences.Set("CoachName",          dto.CoachModelTypeName);
```
El registro (onboarding) nunca pasa por `SaveLoginData` → `"CoachModelTypeName"` queda vacía
hasta el primer login posterior.

---

## Bug secundario — Guard de null incorrecto (línea 103-104)

```csharp
// ACTUAL (bug): solo detecta response == null, ignora response con CoachModelTypeName vacío
if (response == null
    && string.IsNullOrEmpty(response?.CoachModelTypeName))

// CORRECTO: detectar ambos casos
if (response == null
    || string.IsNullOrEmpty(response?.CoachModelTypeName))
```

Con `&&`: si `response != null` pero `CoachModelTypeName` está vacío, la condición es `false`
y el código continúa silenciosamente con datos incompletos.
Con `||`: cualquier fallo en el servidor o campo vacío aborta correctamente.

---

## Archivos afectados

| Archivo | Línea | Cambio |
|---|---|---|
| `ViewModels/CoachModelTypeSelectionViewModel.cs` | 111-112 | Añadir `Preferences.Set("CoachModelTypeName", ...)` |
| `ViewModels/CoachModelTypeSelectionViewModel.cs` | 103-104 | Cambiar `&&` por `\|\|` en el guard de null |

---

## Fix — `[AGENTE: arch-review]`

**Archivo:** `ViewModels/CoachModelTypeSelectionViewModel.cs`

**Cambio 1 — Añadir la clave que lee HomeViewModel:**

```csharp
// ANTES:
Preferences.Set("CoachId",   selectedCoachModelType.Id);
Preferences.Set("CoachName", selectedCoachModelType.Name);

// DESPUÉS:
Preferences.Set("CoachId",             selectedCoachModelType.Id);
Preferences.Set("CoachName",           selectedCoachModelType.Name);
Preferences.Set("CoachModelTypeName",  selectedCoachModelType.Name);  // ← añadir
```

**Cambio 2 — Corregir el guard de null:**

```csharp
// ANTES:
if (response == null
    && string.IsNullOrEmpty(response?.CoachModelTypeName))

// DESPUÉS:
if (response == null
    || string.IsNullOrEmpty(response?.CoachModelTypeName))
```

---

## Checklist de verificación post-fix

1. ✅ Registro nuevo completo → `HomeView` muestra "Pregunta a {nombre del coach elegido}".
2. ✅ Login posterior → sigue mostrando el nombre correcto (sin regresión).
3. ✅ Cambio de usuario (usuario A → usuario B) → botón muestra el coach de B (sin regresión del fix anterior de `_lastUserId`).
4. ✅ `Preferences.Get("CoachModelTypeName")` tiene valor tras `CoachModelTypeSelectionView`.
5. ✅ Si el servidor devuelve `CoachModelTypeName` vacío, el flujo aborta con error en lugar de continuar silenciosamente.
