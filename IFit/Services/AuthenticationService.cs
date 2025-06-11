using IFit.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IFit.Services
{
    public class AuthenticationService
    {
        public async Task LoginAsync(string Username, string Password)
        {
            try
            {
                var request = new SignInRequestDto { Username = Username, Password = Password };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var client = new HttpClient();
                var response = await client.PostAsync(AppSettings.BaseAddress + "/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<SignInResponseDto>(responseBody);


                    if (App.Current?.MainPage != null)
                    {
                        await App.Current.MainPage.DisplayAlert("Bienvenido", loginResponse?.Message, "OK");
                    }

                    // Navegar a la siguiente página
                }
                else
                {
                    if (App.Current?.MainPage != null)
                    {
                        await App.Current.MainPage.DisplayAlert("Error", "Credenciales inválidas", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                if (App.Current?.MainPage != null)
                {
                    await App.Current.MainPage.DisplayAlert("Error", ex.ToString(), "OK");
                }
            }
        }
    }

}
