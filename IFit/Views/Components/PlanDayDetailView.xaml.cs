using IFit.Models.Dtos.AI;

namespace IFit.Views.Components;

public partial class PlanDayDetailView : ContentView
{
    public event EventHandler? OnClose;

    public static readonly BindableProperty TrainingDayDtoProperty =
        BindableProperty.Create(
            nameof(TrainingDayDto),
            typeof(TrainingDayDto),
            typeof(PlanDayDetailView));

    public TrainingDayDto TrainingDayDto
    {
        get => (TrainingDayDto)GetValue(TrainingDayDtoProperty);
        set => SetValue(TrainingDayDtoProperty, value);
    }

    public PlanDayDetailView()
    {
        InitializeComponent();
    }

    public void onCancelClicked(object sender, EventArgs e)
    {
        OnClose?.Invoke(this, EventArgs.Empty);
    }
}