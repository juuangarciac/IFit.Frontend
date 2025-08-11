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

        public async Task<AppUser?> findUserByEmail(string email) 
        {
            var urlAddress = AppSettings.BaseAddress + "/appuser/findByEmail?email=" + Uri.EscapeDataString(email);
            var response = await AppSettings._HttpClient.GetAsync(urlAddress);

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

        public async Task<CoachModelType?> GetSelectedCoachModelTypeByEmail(string email)
        {
            AppUser? appUser = await this.findUserByEmail(email);

            if (AppUser.isPresent(appUser))
            {
                return appUser?.CoachModelType;
            }
            return null;
        }
    }
}
