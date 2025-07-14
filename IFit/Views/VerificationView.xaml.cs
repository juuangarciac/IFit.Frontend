using System.ComponentModel;

namespace IFit.Views;
public partial class VerificationView : ContentPage
{
    public String Name { get; set; } = string.Empty;
    public VerificationView()
    {
        InitializeComponent();
    }
    public async void onCancelClicked(object sender, EventArgs e)
    {
        Console.WriteLine("Cancel clicked. Going back...");
        await Shell.Current.GoToAsync("///MainPage");
    }
}   