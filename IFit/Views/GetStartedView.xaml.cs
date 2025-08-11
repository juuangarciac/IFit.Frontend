namespace IFit.Views;

public partial class GetStartedView : ContentPage
{
	public GetStartedView()
	{
		InitializeComponent();
	}

    public async void onCancelClicked(object sender, EventArgs e)
    {
        Console.WriteLine("Cancel clicked. Going back...");
        await Shell.Current.GoToAsync("///MainPage");
    }

    public async void onContinueClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///CoachModelTypeSelectionView");
    }
}