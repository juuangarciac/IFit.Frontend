namespace IFit.Views;

public partial class ErrorView : ContentPage
{
	public ErrorView()
	{
		InitializeComponent();
	}

    public async void onCancelClicked(object sender, EventArgs e)
    {
        Console.WriteLine("Cancel clicked. Going back...");
        await Shell.Current.GoToAsync("///MainPage");
    }
}