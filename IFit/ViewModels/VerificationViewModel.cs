using IFit.Models.Dtos;
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
        authenticationService = new AuthenticationService();

        LoadUserEmail();
        VerifyEmailCommand = new Command(VerifyEmail);
    }

    private async void LoadUserEmail()
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

    public async void VerifyEmail()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(VerificationCode))
        {
            if(App.Current?.MainPage != null)
            {
                await App.Current.MainPage.DisplayAlert("Error", "Por favor, complete todos los campos.", "OK");

            }
            return;
        }

        EmailValidationResponseDto emailValidationResponse = await authenticationService.VerifyEmail(Email, VerificationCode);

        if (emailValidationResponse == null || !emailValidationResponse.isVerified)
        {
            if (App.Current?.MainPage != null)
            {
                await App.Current.MainPage.DisplayAlert("Error", "Error al verificar el correo electrónico. Inténtalo de nuevo.", "OK");
            }
            return;
        }

        if (App.Current?.MainPage != null)
        {
            await App.Current.MainPage.DisplayAlert("Éxito", "Correo electrónico verificado correctamente.", "OK");
            await Shell.Current.GoToAsync("///SignInView");
        }
    }


    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

}