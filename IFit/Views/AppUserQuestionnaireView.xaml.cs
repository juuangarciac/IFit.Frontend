namespace IFit.Views;

public partial class AppUserQuestionnaireView : ContentPage
{
	public AppUserQuestionnaireView()
	{
		InitializeComponent();
	}
    public async void onCancelClicked(object sender, EventArgs e)
    {
        Console.WriteLine("Cancel clicked. Going back...");
        await Shell.Current.GoToAsync("///MainPage");
    }
}