using IFit.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IFit.ViewModels;

public class VerificationViewModel : INotifyPropertyChanged
{
    private AuthenticationService authenticationService;

    private string _email = string.Empty;
    private string _verificationCode = string.Empty;

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
    public string VerificationCode
    {
        get => _verificationCode;
        set
        {
            if (_verificationCode != value)
            {
                _verificationCode = value;
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

    public ICommand VerifyEmailCommand { get; }

    private async Task VerifyEmail()
    {
        await authenticationService.VerifyEmail(Email, VerificationCode);
    }


    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

}