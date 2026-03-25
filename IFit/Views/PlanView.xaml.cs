namespace IFit.Views;

public partial class PlanView : ContentPage
{
	public PlanView()
	{
		InitializeComponent();
	}

		public async void BackToHome(object sender, EventArgs e)
		{
			Console.WriteLine("Cancel clicked. Going back...");
			await Shell.Current.GoToAsync($"///HomeView");
    }
}