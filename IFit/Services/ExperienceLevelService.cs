using IFit.Models;
using Microsoft.Maui.ApplicationModel.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.Services
{
    public class ExperienceLevelService
    {
        public async Task<List<ExperienceLevel>?> GetExperienceLevels()
        {
            var urlAddress = AppSettings.BaseAddress + "/experiencelevel/findAll";
            var response = await AppSettings._HttpClient.GetAsync(urlAddress);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseData))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<ExperienceLevel>>(responseData);
                }
            }
            return null;
        }
    }
}
