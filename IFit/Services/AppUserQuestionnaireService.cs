using IFit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.Services
{
    public class AppUserQuestionnaireService
    {
        public AppUserQuestionnaireService() { }

        public async Task<AppUserQuestionnaire?> SaveAppUserQuestionnaire(long? userId, long? questionnaireId)
        {
            if (userId == null || userId <= 0 || questionnaireId == null || questionnaireId <= 0)
            {
                return null;
            }

            var urlAddress = AppSettings.BaseAddress + "/userQuestionnaire/saveUserQuestionnaire?userId=" + userId + "&questionnaireId=" + questionnaireId;
            var response = await AppSettings._HttpClient.PostAsync(urlAddress, null);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseData))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<AppUserQuestionnaire>(responseData);
                }
            }
            return null;
        }

        public async Task<AppUserQuestionnaire?> GetUserQuestionnaireByUserIdAsync(long? userId)
        {
            if (userId == null || userId <= 0)
            {
                return null;
            }
            var urlAddress = AppSettings.BaseAddress + "/userQuestionnaire/getUserQuestionnaire?userId=" + userId;
            var response = await AppSettings._HttpClient.GetAsync(urlAddress);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseData))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<AppUserQuestionnaire>(responseData);
                }
            }
            return null;
        }
    }
}
