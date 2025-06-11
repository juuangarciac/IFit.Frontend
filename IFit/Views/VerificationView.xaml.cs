using System.ComponentModel;

namespace IFit.Views;
public partial class VerificationView : ContentPage
{
    public VerificationView()
    {
        InitializeComponent();
    }
    public async void onCancelClicked(object sender, EventArgs e)
    {
        Console.WriteLine("Cancel clicked. Going back...");
        await Shell.Current.GoToAsync("///SignUpView");
    }
}   