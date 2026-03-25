namespace IFit.Views;

public partial class WeeklySummaryView : ContentPage
{
	public WeeklySummaryView()
	{
		InitializeComponent();
	}

    public async void BackToHome(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"///HomeView");
    }
}