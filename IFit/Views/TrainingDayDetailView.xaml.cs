namespace IFit.Views;

public partial class TrainingDayDetailView : ContentPage
{
	public TrainingDayDetailView()
	{
		InitializeComponent();
	}

	public async void Back(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync($"///HomeView");
	}
}