using IFit.Models;
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
        public async Task<List<CoachModelType>?> GetCoachModelTypes()
        {
            try
            {
                var response = await AppSettings._HttpClient.GetAsync(AppSettings.BaseAddress + "/coachmodeltype/findEnabled");
                if(!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Error fetching coach model types: " + response.ReasonPhrase);
                    return null;
                }

                var body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<CoachModelType>>(body);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                await Shell.Current.GoToAsync("//ErrorView");
                return null;
            }
        }

        public async Task<CoachModelType?> GetCoachModelTypeById(string? id)
        {
            try
            {
                if(string.IsNullOrEmpty(id))
                {
                    Console.WriteLine("Error: ID is null or empty.");
                    return null;
                }   

                var response = await AppSettings._HttpClient.GetAsync(AppSettings.BaseAddress + "/coachmodeltype/findById?id=" + id);
                if(!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Error fetching coach model type by ID: " + response.ReasonPhrase);
                    return null;
                }
                var body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CoachModelType>(body);
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
