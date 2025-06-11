namespace IFit.Views;

public partial class SignInView : ContentPage
{
	public SignInView()
	{
		InitializeComponent();
	}

    public async void onCancelClicked(object sender, EventArgs e)
    {
        Console.WriteLine("Cancel clicked. Going back...");
        await Shell.Current.GoToAsync("///MainPage");
    }
}