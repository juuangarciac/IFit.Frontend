using IFit.Models;
using IFit.Models.Dtos;
using System;
using System.Diagnostics;
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

       //  private WebService? WebService = App.GetService<WebService>();

        public async Task<SignInResponseDto?> LoginAsync(string email, string password)
        {
            try
            {
                var request = new SignInRequestDto { email = email, password = password };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await AppSettings._HttpClient.PostAsync(AppSettings.BaseAddress + "/login", content);

                if (!response.IsSuccessStatusCode)
                    return null;

                var body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<SignInResponseDto>(body);
            }
            catch (Exception ex)
            {
                // loggear el error, no solo navegar
                Debug.WriteLine(ex.Message);
                await Shell.Current.GoToAsync("//ErrorView");
                return null;
            }
        }

        public async Task<AppUser?> SignUpAsync(string name, string email, string password)
        {
            try
            {
                   var request = new SignUpRequestDto { name = name, email = email, password = password };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await AppSettings._HttpClient.PostAsync(AppSettings.BaseAddress + "/auth/signup", content);

                if (!response.IsSuccessStatusCode)
                    return null;

                var body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<AppUser>(body);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await Shell.Current.GoToAsync("///ErrorView");
                return null;
            }
        }
        public async Task<EmailValidationResponseDto?> SendVerificationEmail(string email)
        {
            try
            {
                var json = JsonSerializer.Serialize(email);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await AppSettings._HttpClient.PostAsync(AppSettings.BaseAddress + "/auth/sendVerificationEmail", content);

                if (!response.IsSuccessStatusCode)
                    return null;

                var body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EmailValidationResponseDto>(body);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await Shell.Current.GoToAsync("///ErrorView");
                return null;
            }
        }

        public async Task<EmailValidationResponseDto?> VerifyEmail(string email, string verificationCode)
        {
            try
            {
                var request = new EmailValidationRequestDto { email = email, verificationCode = verificationCode };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await AppSettings._HttpClient.PostAsync(AppSettings.BaseAddress + "/auth/verifyEmail", content);

                if (!response.IsSuccessStatusCode)
                    return null;

                var body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<EmailValidationResponseDto>(body);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await Shell.Current.GoToAsync("///ErrorView");
                return null;
            }
        }

        /**
         * 
         * Get access token from preferences or by logging in again
         
        internal async Task<string> GetAccessTokenAsync()
        {
            var token = Preferences.Get("AccessToken", null);
            if (!string.IsNullOrEmpty(token))
                return token;

            if (WebService == null)
                return string.Empty;

            string? email = Preferences.Get("UserEmail", null);
            string? password = Preferences.Get("UserPassword", null);

            if (email == null || password == null)
                return string.Empty;

            var request = new SignInRequestDto { email = email, password = password };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await AppSettings._HttpClient.PostAsync(AppSettings.BaseAddress + "/auth/login", content);

            if (!response.IsSuccessStatusCode)
                return string.Empty;

            var body = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenDTO>(body);

            Preferences.Set("AccessToken", tokenResponse?.AccessToken ?? string.Empty);
            Preferences.Set("RefreshToken", tokenResponse?.RefreshToken ?? string.Empty);
            
            return tokenResponse?.AccessToken ?? string.Empty;
        }

        internal async Task<string> RefreshTokenAsync()
        {
            throw new NotImplementedException();
        }

        */
    }
}
