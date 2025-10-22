using IFit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IFit.Services
{
    public class AppUserAnswerService
    {
        public AppUserAnswerService() { }

        public async Task<AppUserAnswer?> SaveUserAnswer(long? userId, long? questionId, long? questionnaireId, long? answerId)
        {
            if (userId == null || userId <= 0 
                    || questionId == null || questionId <= 0 
                    || questionnaireId == null || questionnaireId <= 0  
                    || answerId == null || answerId <= 0)
            {
                return null;
            }

            var urlAddress = AppSettings.BaseAddress + "/userAnswer/saveUserQuestionAnswer?userId=" + userId + "&questionId=" + questionId + "&questionnaireId=" + questionnaireId + "&answerId=" + answerId;
            var response = await AppSettings._HttpClient.PostAsync(urlAddress, null);
            
            if(response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseData))
                {
                    return JsonSerializer.Deserialize<AppUserAnswer>(responseData);
                }
            }
            return null;
        }

        public async Task<List<AppUserAnswer>?> GetUserAnswersForQuestionnaireAsync(long? userId, long? questionnaireId)
        {
            if (userId == null || userId <= 0 || questionnaireId == null || questionnaireId <= 0)
            {
                return null;
            }
            var urlAddress = AppSettings.BaseAddress + "/userAnswer/getUserAnswersForQuestionnaire?userId=" + userId + "&questionnaireId=" + questionnaireId;
            var response = await AppSettings._HttpClient.GetAsync(urlAddress);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseData))
                {
                    return JsonSerializer.Deserialize<List<AppUserAnswer>>(responseData);
                }
            }
            return null;
        }
    }
}
