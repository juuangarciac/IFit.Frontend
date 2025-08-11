using IFit.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IFit.Services
{
    public class CoachModelTypeService
    {
        public async Task<List<CoachModelTypeDto>?> GetCoachModelTypes()
        {
            try
            {
                var response = await AppSettings._HttpClient.GetAsync(AppSettings.BaseAddress + "/coachmodeltype/findAll");
                if(!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Error fetching coach model types: " + response.ReasonPhrase);
                    return null;
                }

                var body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<CoachModelTypeDto>>(body);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                await Shell.Current.GoToAsync("//ErrorView");
                return null;
            }
        }
    }
}
