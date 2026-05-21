using System.Diagnostics;

namespace IFit.Views;

public partial class CoachModelTypeSelectionView : ContentPage, IQueryAttributable
{
    private string _closeRoute = "///MainPage";

    public CoachModelTypeSelectionView()
    {
        InitializeComponent();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("fromHome"))
            _closeRoute = "///HomeView";
    }

    public async void onCancelClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    public async void onCloseClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(_closeRoute);
}