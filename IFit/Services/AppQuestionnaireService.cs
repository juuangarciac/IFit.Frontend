using IFit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.Services
{
    public class AppQuestionnaireService
    {
        public AppQuestionnaireService() { }
        
        public async Task<List<AppQuestion>?> GetQuestionsForQuestionnaireAsync(long questionnaireId)
        {
            if (questionnaireId <= 0)
            {
                return null;
            }
            var urlAddress = AppSettings.BaseAddress + "/questionnaire/getQuestionsForQuestionnaire?questionnaireId=" + questionnaireId;
            var response = await AppSettings._HttpClient.GetAsync(urlAddress);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseData))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<AppQuestion>>(responseData);
                }
            }
            return null;
        }
    }
}
