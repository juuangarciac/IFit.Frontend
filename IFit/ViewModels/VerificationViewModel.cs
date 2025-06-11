using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace IFit.ViewModels;

public class VerificationViewModel : INotifyPropertyChanged
{
    private string _email = string.Empty;

    public string Email
    {
        get => _email;
        set
        {
            if (_email != value)
            {
                _email = value;
                OnPropertyChanged();
            }
        }
    }

    public VerificationViewModel()
	{
        LoadUserEmail();
	}

    private async Task LoadUserEmail()
    {
        var defaultValue = "NOT_FOUND";
        Email = Preferences.Get("UserEmail", defaultValue);
        Console.WriteLine("UserEmail: " + Email + " found.");

        if (Email == defaultValue)
        {
            await Shell.Current.GoToAsync("///ErrorView");
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

}