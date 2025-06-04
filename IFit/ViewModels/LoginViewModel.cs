using IFit;
using IFit.Models;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace IFit.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        public string username { get; set; }
        public string password { get; set; }

        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new Command(async () => await LoginAsync());
        }

        private async Task LoginAsync()
        {
            try
            {
                var request = new LoginRequest { username = username, password = password };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var client = new HttpClient();
                string BaseAddress = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:8080" : "http://localhost:8080";
                var response = await client.PostAsync(BaseAddress + "/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseBody);

                    await App.Current.MainPage.DisplayAlert("Bienvenido", loginResponse.Message, "OK");
                    // Navegar a la siguiente página
                }
                else
                {
                    await App.Current.MainPage.DisplayAlert("Error", "Credenciales inválidas", "OK");
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Error", ex.ToString(), "OK");
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
