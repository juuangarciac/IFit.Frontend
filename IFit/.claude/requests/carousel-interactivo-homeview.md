# Request: Carousel interactivo en HomeView

## Contexto
El carousel de HomeView usa `CollectionView` con `ItemsLayout="HorizontalList"` y muestra
2 tarjetas (`IFitCard`) hardcodeadas en `HomeViewModel`. Cada tarjeta tiene dos botones
(`TextButtonAccept` / `TextButtonDecline`) pero ambos están sin comando — pulsar no hace nada.
El objetivo es que cualquier botón avance al siguiente card (o no haga nada si ya es el último),
convirtiendo el carousel en interactivo sin cambiar la lógica de negocio ni la estructura de datos.

---

## Tarea 1 — `/arch-review` → `ViewModels/HomeViewModel.cs`

### 1a. Añadir posición del carousel y comando NextCard

```csharp
[ObservableProperty]
public partial int CurrentCarouselPosition { get; set; } = 0;

[RelayCommand]
public void NextCard()
{
    if (CurrentCarouselPosition < InformationCarrousel.Count - 1)
        CurrentCarouselPosition++;
}
```

No se necesita lógica diferente entre botón Accept y Decline: ambos avanzan al siguiente card.

---

## Tarea 2 — `/ui-review` → `Views/HomeView.xaml`

### 2a. Sustituir CollectionView por CarouselView

**Antes** (líneas ~43-133):
```xml
<CollectionView Grid.Row="0"
    ItemsSource="{Binding InformationCarrousel}"
    SelectionMode="None"
    ItemsLayout="HorizontalList">
    <CollectionView.ItemTemplate>
        <DataTemplate x:DataType="items:IFitCard">
            ...
            <!-- Botones sin Command -->
            <Button Text="{Binding TextButtonAccept}" ... />
            <Button Text="{Binding TextButtonDecline}" ... />
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

**Después** — reemplazar la CollectionView completa por:
```xml
<CarouselView Grid.Row="0"
              ItemsSource="{Binding InformationCarrousel}"
              Position="{Binding CurrentCarouselPosition}"
              Loop="False"
              HorizontalScrollBarVisibility="Never">
    <CarouselView.ItemTemplate>
        <DataTemplate x:DataType="items:IFitCard">
            <Border StrokeThickness="2"
                    Stroke="{DynamicResource BackgroundPrimaryDark}"
                    BackgroundColor="{DynamicResource BackgroundPrimaryDark}"
                    Padding="5"
                    Margin="5,0">
                <Border.StrokeShape>
                    <RoundRectangle CornerRadius="{DynamicResource CornerRadiusRectangle}" />
                </Border.StrokeShape>
                <Border.Shadow>
                    <Shadow Brush="{DynamicResource BackgroundSecondaryDark}"
                            Opacity="0.35"
                            Radius="{DynamicResource CornerRadiusRectangle}" />
                </Border.Shadow>

                <!-- Card -->
                <Grid Margin="10"
                      RowSpacing="5"
                      BackgroundColor="Transparent"
                      WidthRequest="315">
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="Auto" />
                    </Grid.RowDefinitions>

                    <!-- Icon + Title -->
                    <Grid Grid.Row="0"
                          ColumnSpacing="5"
                          VerticalOptions="Center">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="Auto" />
                            <ColumnDefinition Width="Auto" />
                        </Grid.ColumnDefinitions>

                        <Image Grid.Column="0"
                               Source="{Binding ImageSource}"
                               HeightRequest="25"
                               WidthRequest="25" />

                        <Label Grid.Column="1"
                               Text="{Binding Title}"
                               FontSize="16"
                               Style="{DynamicResource label-h1-white}" />
                    </Grid>

                    <!-- Text -->
                    <Label Grid.Row="1"
                           Text="{Binding TextBody}"
                           Style="{DynamicResource label-p-white}" />

                    <!-- Buttons -->
                    <Grid Grid.Row="2"
                          ColumnSpacing="10"
                          Margin="0,10,0,0">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="*" />
                        </Grid.ColumnDefinitions>

                        <!-- Accept -->
                        <Button Grid.Column="0"
                                Text="{Binding TextButtonAccept}"
                                FontSize="14"
                                FontAttributes="Italic"
                                Style="{DynamicResource ButtonBold-XL-dark-yellow}"
                                HorizontalOptions="Fill"
                                VerticalOptions="Center"
                                Command="{Binding Source={RelativeSource AncestorType={x:Type vm:HomeViewModel}}, Path=NextCardCommand}" />

                        <!-- Decline -->
                        <Button Grid.Column="1"
                                Text="{Binding TextButtonDecline}"
                                TextColor="{DynamicResource TextPrimaryColorLight}"
                                Style="{DynamicResource BasicButton}"
                                BackgroundColor="Transparent"
                                HorizontalOptions="Fill"
                                VerticalOptions="Center"
                                Command="{Binding Source={RelativeSource AncestorType={x:Type vm:HomeViewModel}}, Path=NextCardCommand}" />
                    </Grid>
                </Grid>
            </Border>
        </DataTemplate>
    </CarouselView.ItemTemplate>
</CarouselView>
```

### 2b. Namespace — ya presente
`xmlns:vm="clr-namespace:IFit.ViewModels"` ya está declarado en HomeView.xaml.
`xmlns:items="clr-namespace:IFit.Resources.Items"` ya está declarado.

### 2c. IndicatorView (opcional)
Si se quiere añadir puntos indicadores debajo del carousel, añadir después del CarouselView
(dentro del mismo Grid.Row="0" con un row adicional, o en Grid.Row="0" via AbsoluteLayout):

```xml
<IndicatorView x:Name="carouselIndicator"
               Grid.Row="0"
               VerticalOptions="End"
               HorizontalOptions="Center"
               IndicatorColor="{DynamicResource TextSecondaryColorLight}"
               SelectedIndicatorColor="{DynamicResource QuaternaryColor}"
               Margin="0,0,0,4" />
```

Y en el CarouselView añadir: `IndicatorView="carouselIndicator"`

---

## Archivos a modificar

| Archivo | Agente | Cambios |
|---|---|---|
| `ViewModels/HomeViewModel.cs` | `/arch-review` | `CurrentCarouselPosition` + `NextCardCommand` |
| `Views/HomeView.xaml` | `/ui-review` | CollectionView → CarouselView, bind botones a NextCardCommand |

---

## Criterios de aceptación

- [ ] Pulsar cualquier botón de la primera card avanza a la segunda card con animación de deslizamiento.
- [ ] Pulsar cualquier botón en la última card no provoca crash ni navegación circular.
- [ ] El carousel se puede deslizar manualmente con swipe (comportamiento nativo de CarouselView).
- [ ] El binding de Command en el DataTemplate resuelve correctamente al HomeViewModel padre.
