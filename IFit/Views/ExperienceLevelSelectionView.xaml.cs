namespace IFit.Views;

public partial class ExperienceLevelSelectionView : ContentPage
{
	public ExperienceLevelSelectionView()
	{
		InitializeComponent();
	}

    public async void onCancelClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    public async void onCloseClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("///MainPage");
}