using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace IFit.ViewModels;

public partial class HelpViewModel : ObservableObject
{
    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("//HomeView");
    }

    [RelayCommand]
    private async Task GoToSupportTicketAsync()
    {
        await Shell.Current.GoToAsync("SupportTicketView");
    }
}
