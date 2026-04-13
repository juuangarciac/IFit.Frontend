namespace IFit.Views;

public partial class QuestionnaireSummaryView : ContentPage
{
	public QuestionnaireSummaryView()
	{
		InitializeComponent();
	}

    public async void onCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///MainPage", false);
    }
}