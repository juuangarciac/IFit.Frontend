using System.Diagnostics;

namespace IFit.Views;

public partial class CoachModelTypeSelectionView : ContentPage
{
	public CoachModelTypeSelectionView()
	{
        InitializeComponent();
    }
    public async void onCancelClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    public async void onCloseClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("///MainPage");
}