# IFit — Guía de estilo visual canónica

> **Fuente de verdad para decisiones de UI.** Todos los agentes (`/ui-review`, `/arch-review`, `/bug-report`) deben leer este archivo antes de tocar cualquier vista o emitir cualquier juicio visual. La última versión aplicada en código manda sobre cualquier convención anterior.

---

## 1. Paleta de colores

| Token | Hex | Uso |
|---|---|---|
| `BackgroundPrimaryDark` | `#222831` | Fondo de página, Shell, fondo de card seleccionada |
| `BackgroundSecondaryDark` | `#393E46` | **Fondo estándar de cards** (estado normal) |
| `TertiaryColor` | `#FFD369` | Amarillo — CTAs principales, highlights, badge ACTIVAR |
| `TextPrimaryColorLight` | `#EEEEEE` | Texto sobre fondos oscuros |
| `TextSecondaryColorLight` | `#F5F5F5` | Texto secundario, separadores |
| `BackgroundPrimaryLight` | `#EEEEEE` | Inputs, superficies claras |
| `PremiumGradient` | azul→oscuro (vertical) | Fondo del HomeView |
| `CardPremiumGradientColor` | cyan→verde→`#222831` | Barra lateral: entrenamiento activo / completado |
| `CardPremiumIndigoGradientColor` | índigo→azul→oscuro | Barra lateral: sesiones pendientes |
| `CardPremiumYellowGradientColor` | amarillo→dorado→`#222831` | Header de PlanSummaryView, barra lateral destacada |

**Regla**: nunca usar valores hex inline en XAML. Siempre `{StaticResource TokenName}`.

---

## 2. Tipografía

| Estilo | FontFamily | Size | Uso |
|---|---|---|---|
| `label-Title-white` | Montserrat-Bold | 26 | Título principal de pantalla |
| `label-h1-white` | Montserrat-Bold | 18 | Sección principal |
| `label-h2-white` | Montserrat-Bold | 16 | Subsección, nombre de card |
| `label-h3-white` | Montserrat-Bold | 14 | Etiqueta pequeña |
| `label-p-white` | Montserrat-Medium | 14 | Cuerpo de texto, metadatos |
| `navbar-p-white` | Montserrat-Medium | 10 | Etiquetas de navbar |

### Spans dentro de FormattedString

Los `Span` **no heredan `FontFamily` del Label padre** aunque el Label tenga un estilo. Hay que declararlo siempre explícitamente:

```xml
<!-- MAL: el Span usará la fuente del sistema, no Montserrat -->
<Span Text="Series: " FontAttributes="Bold"/>

<!-- BIEN -->
<Span Text="Series: " FontFamily="Montserrat-Bold"/>
<Span Text="{Binding Sets}"/>   <!-- hereda Montserrat-Medium del estilo global de Span -->
```

El estilo global de `Span` en `Styles.xaml` ya fija `TextColor=White` y `FontFamily=Montserrat-Medium` como base. Solo hay que sobreescribir en spans que sean negrita o de un color diferente.

---

## 3. Cards — El patrón canónico

### Card estándar (sin selección)

```xml
<Border StrokeThickness="0"
        Margin="0,0,0,8"
        BackgroundColor="{StaticResource BackgroundSecondaryDark}">
    <Border.StrokeShape>
        <RoundRectangle CornerRadius="8"/>
    </Border.StrokeShape>
    <Grid Padding="12,10" RowSpacing="5">
        <!-- contenido -->
    </Grid>
</Border>
```

### Card seleccionable (dentro de CollectionView)

```xml
<Border StrokeThickness="0">
    <VisualStateManager.VisualStateGroups>
        <VisualStateGroupList>
            <VisualStateGroup x:Name="CommonStates">
                <VisualState x:Name="Normal">
                    <VisualState.Setters>
                        <Setter Property="BackgroundColor" Value="{StaticResource BackgroundSecondaryDark}" />
                    </VisualState.Setters>
                </VisualState>
                <VisualState x:Name="Selected">
                    <VisualState.Setters>
                        <Setter Property="BackgroundColor" Value="{StaticResource BackgroundPrimaryDark}" />
                    </VisualState.Setters>
                </VisualState>
            </VisualStateGroup>
        </VisualStateGroupList>
    </VisualStateManager.VisualStateGroups>
    <Border.StrokeShape>
        <RoundRectangle CornerRadius="8"/>
    </Border.StrokeShape>
    <Grid Padding="12,10">
        <!-- contenido -->
    </Grid>
</Border>
```

### Card con barra lateral de color (sesiones / días)

La barra lateral de 24 dp es un **indicador semántico de estado**, no decoración. Se conserva siempre:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="24"/>
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>

    <!-- Barra lateral — color según estado -->
    <Border Grid.Column="0"
            StrokeThickness="0"
            Margin="0"
            Background="{DynamicResource CardPremiumGradientColor}">
        <!-- Verde: activo/completado -->
        <!-- CardPremiumIndigoGradientColor: pendiente -->
        <!-- CardPremiumYellowGradientColor: destacado -->
        <Border.StrokeShape>
            <RoundRectangle CornerRadius="8,0,8,0"/>
        </Border.StrokeShape>
    </Border>

    <!-- Contenido -->
    <Grid Grid.Column="1" Padding="12,10" RowSpacing="4">
        <!-- ... -->
    </Grid>
</Grid>
```

### Card de descripción / texto informativo

```xml
<Border StrokeThickness="0"
        Margin="0,0,0,16"
        BackgroundColor="{StaticResource BackgroundSecondaryDark}">
    <Border.StrokeShape>
        <RoundRectangle CornerRadius="8"/>
    </Border.StrokeShape>
    <Label Text="{Binding Description}"
           Style="{StaticResource label-p-white}"
           Padding="12,10" />
</Border>
```

---

## 4. Lo que está PROHIBIDO en cards

| Prohibido | Motivo | Alternativa |
|---|---|---|
| `StrokeThickness="1"` o cualquier valor > 0 | Genera bordes blancos que distraen | `StrokeThickness="0"` |
| `Stroke="{StaticResource BackgroundPrimaryLight}"` | Borde blanco visible | Eliminar |
| `<Border.Shadow>` dentro de cards de lista | Ruido visual innecesario | Eliminar |
| Accent strip (borde de 3 px horizontal arriba) | Elemento repetitivo y distractor | Eliminar |
| `<BoxView>` separador vertical dentro de card | Barra blanca interna que fragmenta | Eliminar |
| `CornerRadius` > 8 en cards de lista | Inconsistencia | `CornerRadius="8"` |
| `BackgroundColor=BackgroundPrimaryDark` en estado Normal del VSM | El fondo primario es para seleccionado, no normal | `BackgroundSecondaryDark` para Normal |

---

## 5. Estructura de header de pantalla

```xml
<!-- Header con gradiente + navbar + metadatos -->
<Grid Grid.Row="0"
      VerticalOptions="Start"
      Background="{StaticResource CardPremiumXxxGradientColor}">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="Auto" />
    </Grid.RowDefinitions>

    <!-- Navbar: flecha izquierda + logo centrado -->
    <Grid Grid.Row="0" Margin="15">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>
        <Button Grid.Column="0"
                HorizontalOptions="Start"
                VerticalOptions="Center"
                BackgroundColor="Transparent"
                ImageSource="arrow.png"
                Command="{Binding GoBackCommand}" />
        <Image Grid.Column="0"
               Grid.ColumnSpan="2"
               Source="ifit_logo_white.png"
               HeightRequest="20"
               WidthRequest="120"
               HorizontalOptions="Center"
               VerticalOptions="Center" />
    </Grid>

    <!-- Metadatos: eyebrow + título + info adicional -->
    <VerticalStackLayout Grid.Row="1"
                         Margin="15,0,15,20"
                         Spacing="4">
        <Label Text="EYEBROW LABEL"
               FontSize="11"
               CharacterSpacing="2"
               Opacity="0.7"
               Style="{StaticResource label-p-white}" />
        <Label Text="{Binding Title}"
               Style="{StaticResource label-Title-white}" />
        <!-- info adicional con Opacity="0.7/0.8" -->
    </VerticalStackLayout>
</Grid>
```

---

## 6. Separadores de sección

```xml
<!-- Separador entre header y body -->
<BoxView HeightRequest="1"
         BackgroundColor="{StaticResource TextSecondaryColorLight}"
         Margin="0,0,0,12" />

<!-- Título de sección -->
<Label Text="Nombre sección"
       Style="{StaticResource label-h2-white}"
       Margin="0,0,0,8" />
```

---

## 7. Spacing y márgenes

| Elemento | Valor |
|---|---|
| Margen lateral de contenido scrollable | `Margin="15,12,15,0"` o `Padding="15"` |
| Separación entre cards en lista | `Margin="0,0,0,8"` en cada card |
| Padding interno de card | `Padding="12,10"` |
| Separación entre secciones | `Spacing="16"` o `Margin="0,0,0,16"` después de la última card |
| `RowSpacing` en Grid de card | `5` (compacto) |

---

## 8. Botones

| Estilo | Fondo | Texto | Uso |
|---|---|---|---|
| `ButtonBold-XL-yellow` | `TertiaryColor` | oscuro | CTA principal |
| `ButtonBold-XL-dark-white` | `BackgroundSecondaryDark` | claro | Acción secundaria |
| `ButtonBold-XL-dark-yellow` | `BackgroundSecondaryDark` | amarillo | Énfasis secundario |
| Botón navbar (flecha/cancelar) | Transparente | — | Solo `ImageSource`, `BackgroundColor="Transparent"` |

---

## 9. Páginas de referencia — ya estilizadas correctamente

Estas vistas representan el patrón visual oficial. Ante cualquier duda, abrirlas y copiar su estructura:

| Vista | Patrón de referencia para |
|---|---|
| `TrainingDayDetailView.xaml` | Cards sin borde, layout de sección, header con gradiente |
| `PlanSummaryView.xaml` | Cards seleccionables, VisualStateManager correcto |
| `WeeklySummaryView.xaml` | Cards con barra lateral, header con imagen de fondo |
| `HomeView.xaml` | Cards con barra lateral en vista principal |
