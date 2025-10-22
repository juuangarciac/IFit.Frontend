using IFit.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.Services
{
    public class AnswerService
    {

        public AnswerService() { }

        public async Task<List<AppAnswer>?> findAnswersByQuestionId(long questionId)
        {
            if (questionId <= 0)
            {
                return null;
            }
            var urlAddress = AppSettings.BaseAddress + "/appanswer/findAnswersByQuestionId?questionId=" + questionId;
            var response = await AppSettings._HttpClient.GetAsync(urlAddress);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseData))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<AppAnswer>>(responseData);
                }
            }
            return null;
        }
    }
}
