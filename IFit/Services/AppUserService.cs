using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFit.Models;
using IFit.Models.Dtos;

namespace IFit.Services
{
    public class AppUserService
    {
        public AppUserService()  { }

        public async Task<AppUser> findUserByEmail(string email) 
        {
            var request = new EmailValidationRequestDto { Email = email };
            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = new HttpClient();
            var response = await client.PostAsync(AppSettings.BaseAddress + "/appuser/findByEmail", content);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                if(!string.IsNullOrEmpty(responseData))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<AppUser>(responseData);
                }
            }
            return null;
        }
    }
}
