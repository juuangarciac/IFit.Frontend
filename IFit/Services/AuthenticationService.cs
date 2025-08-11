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
                Console.WriteLine(ex.Message);
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
    }
}
