using IFit.ViewModels;

namespace IFit.Views;

public partial class PlanView : ContentPage
{
    public PlanView()
    {
        InitializeComponent();
        DayDetailView.OnClose += (s, e) =>
        {
            if (BindingContext is PlanViewModel vm)
            {
                vm.CloseDetail();
            }
        };
    }

    public async void BackToHome(object sender, EventArgs e)
		{
			Console.WriteLine("Cancel clicked. Going back...");
			await Shell.Current.GoToAsync($"//PlanSummaryView");
    }
}