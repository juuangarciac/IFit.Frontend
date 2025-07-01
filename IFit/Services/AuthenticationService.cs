using IFit.Models;
using IFit.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IFit.Services
{
    public class AuthenticationService
    {
        public async Task<SignInResponseDto> LoginAsync(string Email, string Password)
        {
            try
            {
                var request = new SignInRequestDto { email = Email, password = Password };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var client = new HttpClient();
                var response = await client.PostAsync(AppSettings.BaseAddress + "/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<SignInResponseDto>(responseBody);

                    if(loginResponse == null)
                    {
                        return null;
                    }

                    return loginResponse;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.GoToAsync("///ErrorView");
                return null;
            }
        }
        public async Task SignUpAsync(string Name, string Email, string Password)
        {
            try
            {
                var request = new SignUpRequestDto { name = Name, email = Email, password = Password };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var client = new HttpClient();
                var response = await client.PostAsync(AppSettings.BaseAddress + "/auth/signup", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonSerializer.Deserialize<AppUser>(responseBody);


                    if (App.Current?.MainPage != null)
                    {
                        await App.Current.MainPage.DisplayAlert("OK", "Usuario registrado", "OK");
                    }
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
                Console.WriteLine("Error", ex.ToString(), "OK");
                await Shell.Current.GoToAsync("///ErrorView");
            }
        }
        public async Task SendVerificationEmail(string Email)
        {
            try
            {
                var request = Email;
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var client = new HttpClient();
                var response = await client.PostAsync(AppSettings.BaseAddress + "/auth/sendVerificationEmail", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var serverResponse = JsonSerializer.Deserialize<String>(responseBody);


                    if (App.Current?.MainPage != null)
                    {
                        await App.Current.MainPage.DisplayAlert("OK", serverResponse?.ToString(), "OK");
                    }
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
                    Console.WriteLine("Error", ex.ToString(), "OK");
                    await Shell.Current.GoToAsync("///ErrorView");
                }
            }
        }

        public async Task<EmailValidationResponseDto> VerifyEmail(string email, string verificationCode)
        {
            try
            {
                var request = new EmailValidationRequestDto
                {
                    email = email,
                    verificationCode = verificationCode
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var client = new HttpClient();
                var response = await client.PostAsync(AppSettings.BaseAddress + "/auth/verifyEmail", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var verificationResponse = JsonSerializer.Deserialize<EmailValidationResponseDto>(responseBody);

                    if (verificationResponse != null )
                    {
                        return verificationResponse;
                    }

                    return new EmailValidationResponseDto
                    {
                        isVerified = false,
                        message = "No se ha podido realizar la verificación.",
                        email = email
                    };
                }
                else
                {
                    return new EmailValidationResponseDto
                    {
                        isVerified = false,
                        message = "No se ha podido realizar la verificación.",
                        email = email
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);

                if (App.Current?.MainPage != null)
                {
                    await Shell.Current.GoToAsync("///ErrorView");
                }

                return new EmailValidationResponseDto
                {
                    isVerified = false,
                    message = "Error inesperado: " + ex.Message,
                    email = email
                };
            }
        }
    }
}
