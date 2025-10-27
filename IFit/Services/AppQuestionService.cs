using IFit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IFit.Services
{
    public class AppQuestionService
    {
        public AppQuestionService() { }

        public async Task<AppQuestion> findQuestionById(long questionId)
        {
            if (questionId <= 0)
            {
                return null;
            }
            var urlAddress = AppSettings.BaseAddress + "/appquestion/findQuestionById?questionId=" + questionId;
            var response = await AppSettings._HttpClient.GetAsync(urlAddress);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseData))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<AppQuestion>(responseData);
                }
            }
            return null;
        }

        public async Task<List<AppQuestion>?> findQuestionsByQuestionnaireId(long questionnaireId)
        {
            if (questionnaireId <= 0)
            {
                return null;
            }

            var urlAddress = AppSettings.BaseAddress + "/question/findQuestionsByQuestionnaireId?questionnaireId=" + questionnaireId;
            var response = await AppSettings._HttpClient.GetAsync(urlAddress);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseData))
                {
                    return JsonSerializer.Deserialize<List<AppQuestion>>(responseData);
                }
            }
            return null;
        }
    }
}
