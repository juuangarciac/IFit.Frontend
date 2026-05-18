# Request: Bug fixes HomeView — carousel loop, loading overlay en navegación, limpieza header

## Bugs reportados

---

## Bug 1 — Carousel: último card no vuelve al primero

**Problema**: `NextCard()` en `HomeViewModel` tiene la condición:
```csharp
if (CurrentCarouselPosition < InformationCarrousel.Count - 1)
    CurrentCarouselPosition++;
// si ya es el último → no hace nada
```
Pulsar un botón en el último card no produce ningún efecto visible.

**Fix** — `/arch-review` → `ViewModels/HomeViewModel.cs`:
```csharp
[RelayCommand]
public void NextCard()
{
    if (CurrentCarouselPosition < InformationCarrousel.Count - 1)
        CurrentCarouselPosition++;
    else
        CurrentCarouselPosition = 0; // volver al primer card
}
```

---

## Bug 2 — No hay feedback de carga al pulsar los botones de HomeView

**Problema**: `IsLoading` solo se activa durante `InitializeAsync` (carga inicial). Los comandos
de navegación (`OpenTrainingDayDetailCommand`, `GoToWeeklySummaryCommand`, `GoToChatViewCommand`)
no activan el overlay → la UI parece congelada mientras Shell hace la transición.

**Fix** — `/arch-review` → `ViewModels/HomeViewModel.cs`:
Envolver cada navegación con `IsLoading = true / false`:

```csharp
[RelayCommand]
public async Task OpenTrainingDayDetailAsync()
{
    IsLoading = true;
    var navigationParameter = new Dictionary<String, Object>()
    {
        {"Routine", Routine },
        {"TrainingDay", TrainingDayDto }
    };
    await Shell.Current.GoToAsync("TrainingDayDetailView", navigationParameter);
    IsLoading = false;
}

[RelayCommand]
public async Task GoToWeeklySummaryAsync()
{
    if (Routine == null) return;
    IsLoading = true;
    var navigationParameter = new Dictionary<string, object>()
    {
        { "Routine", Routine }
    };
    await Shell.Current.GoToAsync("WeeklySummaryView", navigationParameter);
    IsLoading = false;
}

[RelayCommand]
public async Task GoToChatViewAsync()
{
    IsLoading = true;
    await Shell.Current.GoToAsync("ChatAIView");
    IsLoading = false;
}
```

`IsLoading = false` se ejecuta en background sobre HomeViewModel mientras se muestra la página
de destino; cuando el usuario vuelve con pop, el overlay ya está oculto.

---

## Bug 3 — Header: eliminar calendario y mover notificación a la derecha

**Problema**: `HomeHeader.xaml` muestra:
- Izquierda: [Perfil, Notificaciones]
- Centro: Logo ifit
- Derecha: Icono calendario (Image sin botón)
- Row 1: Componente `cal:Calendar`
- Row 2: BoxView "expandir calendario"

Se debe eliminar el calendario y mover el icono de notificación a la derecha.

**Resultado esperado**:
- Izquierda: [Perfil] (con botón ripple existente)
- Centro: Logo ifit
- Derecha: [Notificaciones]
- Sin filas de calendario ni indicador de expansión

**Fix** — `/ui-review` → `Views/Components/HomeHeader.xaml`:
- Quitar `xmlns:cal` del namespace
- Simplificar `Grid.RowDefinitions` a una sola fila
- Quitar `StackLayout` de la izquierda → solo el Grid de perfil
- Mover el `Image Source="notification_icon_white.png"` a la columna derecha (donde estaba el calendario)
- Eliminar `<cal:Calendar>` (Grid.Row="1") y el `<BoxView>` de expansión (Grid.Row="2")

**Fix** — `/arch-review` → `ViewModels/Components/HomeHeaderViewModel.cs`:
- Eliminar `using Plugin.Maui.Calendar.Controls` y `using Plugin.Maui.Calendar.Enums`
- Eliminar `SelectedLayout`, `ExpandCalendarSelected`, `OnExpandCalendarSelected`
- La clase puede quedar vacía o eliminarse si el XAML ya no necesita BindingContext
  (el único comando activo en el header — GoToProfile — usa `RelativeSource AncestorType`)

---

## Archivos a modificar

| Archivo | Agente | Cambios |
|---|---|---|
| `ViewModels/HomeViewModel.cs` | `/arch-review` | Bug 1: NextCard loop. Bug 2: IsLoading en navegación |
| `ViewModels/Components/HomeHeaderViewModel.cs` | `/arch-review` | Bug 3: Eliminar dependencias y lógica de calendario |
| `Views/Components/HomeHeader.xaml` | `/ui-review` | Bug 3: Eliminar cal:Calendar, mover notificación a derecha |

---

## Criterios de aceptación

- [ ] Pulsar un botón en el último card del carousel vuelve al primer card con animación.
- [ ] Al pulsar "Entrenamiento de hoy" aparece el overlay de carga durante la transición.
- [ ] Al pulsar "Resumen de la semana" aparece el overlay de carga durante la transición.
- [ ] Al pulsar "Pregunta a Ronnie" aparece el overlay de carga durante la transición.
- [ ] El header no muestra ni el calendario ni el indicador de expansión.
- [ ] El icono de notificaciones aparece en la esquina derecha del header.
- [ ] El icono de perfil (izquierda) sigue funcionando con ripple.
