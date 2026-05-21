# Request: Rediseño TrainingDayDetailView — coherencia con estética HomeView

## Contexto

La vista muestra información correcta pero usa un lenguaje visual propio (shadow, borders con
stroke, fondo semitransparente, franja de color en la parte superior del card, número grande
en cursiva) que no coincide con el patrón limpio de HomeView / WeeklySummaryView.

**Principio rector**: menos estilos distintos, más consistencia. El usuario ya reconoce el
patrón visual de HomeView; esta vista debe sentirse parte de la misma app.

---

## Patrón de card de HomeView (referencia única)

```
Border  StrokeThickness="0"  BackgroundColor="BackgroundPrimaryDark"  CornerRadius="8"
└── Grid [24px | *]
    ├── Column 0: Border con franja de color (CornerRadius="8,0,8,0")
    └── Column 1: Grid Padding="12,10" RowSpacing="10"
         ├── label-h2-white  ← título
         └── label-p-white   ← subtítulo / descripción
```

Sin shadow. Sin stroke de color. Sin fondos semitransparentes. Fondo siempre `BackgroundPrimaryDark`.

---

## Tarea 1 — `/arch-review` → `ViewModels/TrainingDayDetailViewModel.cs`

### 1a. Añadir IsLoading + StatusMessage

`EndSessionAsync` hace una llamada HTTP sin overlay. Patrón estándar de la app:

```csharp
[ObservableProperty]
public partial Boolean IsLoading { get; set; } = false;

[ObservableProperty]
public partial String StatusMessage { get; set; } = string.Empty;
```

`EndSessionAsync` actualizado:
```csharp
[RelayCommand]
public async Task EndSessionAsync()
{
    IsLoading = true;
    StatusMessage = "Guardando sesión...";
    try
    {
        var response = await _trainingService.setRoutineDayAsCompletedAsync(
            (long)Routine.Id, (int)Routine.CurrentDay);

        if (response == null)
        {
            await NotificationService.ShowErrorAsync("No se pudo finalizar la sesión.");
            return;
        }

        await NotificationService.ShowSuccessAsync("¡Sesión guardada correctamente!");
        await Shell.Current.GoToAsync("//HomeView", false);
    }
    finally
    {
        IsLoading = false;
        StatusMessage = string.Empty;
    }
}
```

### 1b. Añadir ExerciseCount y SessionStatusMessage

```csharp
[ObservableProperty]
public partial int ExerciseCount { get; set; }

[ObservableProperty]
public partial string SessionStatusMessage { get; set; } = string.Empty;
```

Calculados en `OnTrainingDayChanged`:
```csharp
partial void OnTrainingDayChanged(TrainingDayDto value)
{
    ExerciseCount = value?.Exercises?.Count ?? 0;
    EstimatedDuration = CalculateEstimatedDuration(value);
    CanEndSession = Routine.CurrentDay == value.DayNumber;
    Background = GetBrush(Routine.CurrentDay >= value.DayNumber
        ? "CardPremiumGradientColor"
        : "CardPremiumIndigoGradientColor");

    if (Routine.CurrentDay > value.DayNumber)
        SessionStatusMessage = "Sesión ya completada";
    else if (Routine.CurrentDay < value.DayNumber)
        SessionStatusMessage = "Aún no has llegado a este entrenamiento";
    else
        SessionStatusMessage = string.Empty;
}
```

### 1c. Corregir CalculateEstimatedDuration — mínimo 30 minutos

La fórmula actual produce duraciones demasiado bajas (5 ejercicios × 3 series → ~20 min).
Causas: `secondsPerSet` demasiado bajo (40 s) y sin tiempo de transición entre ejercicios.

```csharp
private string CalculateEstimatedDuration(TrainingDayDto trainingDay)
{
    if (trainingDay?.Exercises == null || trainingDay.Exercises.Count == 0)
        return "~30 min";

    const int secondsPerSet = 50;       // tiempo de ejecución por serie
    const int transitionSeconds = 90;   // calentamiento/transición entre ejercicios

    int totalSeconds = 0;

    foreach (var exercise in trainingDay.Exercises)
    {
        int sets = exercise.Sets ?? 0;
        int rest = exercise.RestSeconds ?? 60;
        if (sets <= 0) continue;

        int executionTime = sets * secondsPerSet;
        int restBetweenSets = (sets - 1) * rest;
        totalSeconds += executionTime + restBetweenSets + transitionSeconds;
    }

    // Mínimo garantizado de 30 minutos
    int minutes = Math.Max(totalSeconds / 60, 30);

    if (minutes < 60)
        return $"~{minutes} min";

    int hours = minutes / 60;
    int remaining = minutes % 60;
    return remaining > 0 ? $"~{hours}h {remaining}min" : $"~{hours}h";
}
```

---

## Tarea 2 — `/ui-review` → `Views/TrainingDayDetailView.xaml`

### 2a. LoadingOverlay

Añadir al final del Grid raíz (el namespace `components` ya existe en la vista):

```xml
<components:LoadingOverlay
    Grid.RowSpan="3"
    IsLoading="{Binding IsLoading}"
    Message="{Binding StatusMessage}" />
```

### 2b. Simplificar la sección Descripción

Eliminar el card complejo actual (franja superior, número grande en cursiva, separador vertical)
y sustituirlo por el patrón de card estándar de la app:

```xml
<Label Text="Descripción" Style="{StaticResource label-h2-white}" />

<Border StrokeThickness="0"
        BackgroundColor="{DynamicResource BackgroundPrimaryDark}">
    <Border.StrokeShape>
        <RoundRectangle CornerRadius="8"/>
    </Border.StrokeShape>
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="24"/>
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>
        <Border Grid.Column="0"
                StrokeThickness="0"
                Margin="0"
                Background="{Binding Background}">
            <Border.StrokeShape>
                <RoundRectangle CornerRadius="8,0,8,0"/>
            </Border.StrokeShape>
        </Border>
        <Label Grid.Column="1"
               Text="{Binding TrainingDay.Description}"
               Style="{DynamicResource label-p-white}"
               Padding="12,10" />
    </Grid>
</Border>
```

### 2c. Rediseñar cards de ejercicios con el patrón HomeView

Eliminar: `StrokeThickness="2"`, `Stroke`, `Shadow`, `BackgroundColor=PrimaryColorLightOpacity`.
Aplicar el patrón estándar (franja izquierda + contenido limpio):

```xml
<Border StrokeThickness="0"
        Margin="0,0,0,8"
        BackgroundColor="{DynamicResource BackgroundPrimaryDark}">
    <Border.StrokeShape>
        <RoundRectangle CornerRadius="8"/>
    </Border.StrokeShape>
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="24"/>
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>

        <!-- Franja de color (verde si es sesión actual/pasada, índigo si es futura) -->
        <Border Grid.Column="0"
                StrokeThickness="0"
                Margin="0"
                Background="{Binding Source={RelativeSource AncestorType={x:Type ContentPage}},
                             Path=BindingContext.Background}">
            <Border.StrokeShape>
                <RoundRectangle CornerRadius="8,0,8,0"/>
            </Border.StrokeShape>
        </Border>

        <!-- Contenido -->
        <Grid Grid.Column="1" Padding="12,10" RowSpacing="6">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>  <!-- Nombre -->
                <RowDefinition Height="Auto"/>  <!-- Notas (oculto si vacío) -->
                <RowDefinition Height="Auto"/>  <!-- Sets / Reps / Descanso -->
            </Grid.RowDefinitions>

            <Label Grid.Row="0"
                   Text="{Binding ExerciseName}"
                   Style="{DynamicResource label-h2-white}" />

            <Label Grid.Row="1"
                   Text="{Binding Notes}"
                   Style="{DynamicResource label-p-white}"
                   Opacity="0.7"
                   IsVisible="{Binding Notes,
                       Converter={StaticResource StringNotNullOrEmptyConverter}}" />

            <!-- Stats: Sets · Reps · Descanso -->
            <HorizontalStackLayout Grid.Row="2" Spacing="16">
                <Label Style="{DynamicResource label-p-white}">
                    <Label.FormattedText>
                        <FormattedString>
                            <Span Text="Series: " FontAttributes="Bold"/>
                            <Span Text="{Binding Sets}"/>
                        </FormattedString>
                    </Label.FormattedText>
                </Label>
                <Label Style="{DynamicResource label-p-white}">
                    <Label.FormattedText>
                        <FormattedString>
                            <Span Text="Reps: " FontAttributes="Bold"/>
                            <Span Text="{Binding Reps}"/>
                        </FormattedString>
                    </Label.FormattedText>
                </Label>
                <Label Style="{DynamicResource label-p-white}">
                    <Label.FormattedText>
                        <FormattedString>
                            <Span Text="Descanso: " FontAttributes="Bold"/>
                            <Span Text="{Binding RestSeconds}"/>
                            <Span Text=" seg"/>
                        </FormattedString>
                    </Label.FormattedText>
                </Label>
            </HorizontalStackLayout>
        </Grid>
    </Grid>
</Border>
```

Cambios respecto al original:
- Franja izquierda en lugar de franja superior
- Sin shadow ni stroke de color
- Fondo `BackgroundPrimaryDark` en lugar de semitransparente
- `label-h2-white` en lugar de `label-h3-white` para el nombre (más legible)
- `Sets` → "Series" (más claro en castellano)
- Descanso con unidad "seg"
- Notes oculto si vacío (sin espacio en blanco)
- `HorizontalStackLayout` en lugar de Grid de 3 columnas (más limpio, no requiere ancho fijo)

### 2d. Añadir conteo de ejercicios en la cabecera

Junto a la duración estimada, una línea extra sencilla:

```xml
<HorizontalStackLayout Spacing="10">
    <Label Text="{Binding ExerciseCount, StringFormat='{0} ejercicios'}"
           Style="{StaticResource label-p-white}"
           Opacity="0.8" />
</HorizontalStackLayout>
```

### 2e. Mensaje contextual en lugar de botón invisible

Cuando `CanEndSession = false`, el botón desaparece sin explicación:

```xml
<Grid Grid.Row="2" Margin="15">
    <Button Text="Finalizar sesión"
            Style="{StaticResource ButtonBold-XL-yellow}"
            CornerRadius="{StaticResource CornerRadiusRectangle}"
            Command="{Binding EndSessionCommand}"
            IsEnabled="{Binding CanEndSession}"
            IsVisible="{Binding CanEndSession}" />

    <Label Text="{Binding SessionStatusMessage}"
           Style="{StaticResource label-p-white}"
           HorizontalTextAlignment="Center"
           Opacity="0.6"
           IsVisible="{Binding CanEndSession,
               Converter={StaticResource InvertedBoolConverter}}" />
</Grid>
```

### 2f. Corregir typo

`"Descripcion"` → `"Descripción"`

---

## Archivos a modificar

| Archivo | Agente | Cambios |
|---|---|---|
| `ViewModels/TrainingDayDetailViewModel.cs` | `/arch-review` | IsLoading, StatusMessage, ExerciseCount, SessionStatusMessage, EndSessionAsync con overlay, CalculateEstimatedDuration con mínimo 30 min |
| `Views/TrainingDayDetailView.xaml` | `/ui-review` | LoadingOverlay, descripción con card estándar, ejercicios con patrón HomeView, conteo ejercicios, mensaje contextual, typo |

---

## Criterios de aceptación

- [ ] Los cards de ejercicio siguen exactamente el patrón visual de HomeView (franja izquierda, sin shadow, fondo oscuro).
- [ ] La sección Descripción usa el mismo tipo de card (franja izquierda + texto).
- [ ] Al pulsar "Finalizar sesión" aparece el overlay de carga.
- [ ] La duración estimada nunca muestra menos de 30 minutos.
- [ ] El campo Descanso muestra la unidad "seg".
- [ ] Las notas vacías no generan espacio en blanco.
- [ ] Cuando el día está completado o es futuro, aparece un mensaje explicativo en lugar de un botón invisible.
- [ ] La cabecera muestra el número total de ejercicios.
