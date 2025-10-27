using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFit.Models;
using IFit.Services;

namespace IFit.Services
{
    public class AIRoutineService
    {
        private AppUserService? appUserService;
        private AppUserQuestionnaireService? appUserQuestionnaireService;
        private AppUserAnswerService? appUserAnswerService;
        private AppQuestionService? appQuestionService;
        private AnswerService? appAnswerService;
        private CoachModelTypeService? coachModelTypeService;

        public AIRoutineService()
        {
            appUserService = App.GetService<AppUserService>();
            appUserQuestionnaireService = App.GetService<AppUserQuestionnaireService>();
            appUserAnswerService = App.GetService<AppUserAnswerService>();
            appQuestionService = App.GetService<AppQuestionService>();
            appAnswerService = App.GetService<AnswerService>();
            coachModelTypeService = App.GetService<CoachModelTypeService>();
        }


        public async Task<string?> CreateRoutinePromptForAI(AppUser user)
        {
            if (appUserService == null
                || appUserQuestionnaireService == null
                || appUserAnswerService == null
                || appQuestionService == null
                || appAnswerService == null)
            {
                return null;
            }

            var questionnaire = await appUserQuestionnaireService.GetUserQuestionnaireByUserIdAsync(user.Id);
            if (questionnaire == null)
            {
                return null;
            }

            var userAnswersIds = await appUserAnswerService.GetUserAnswersForQuestionnaireAsync(questionnaire.Id, user.Id);
            if (userAnswersIds == null || userAnswersIds.Count == 0)
            {
                return null;
            }

            var prompt = "Genera una rutina fitness basada en la siguiente información del usuario:\n";
            foreach (var userAnswer in userAnswersIds)
            {
                var appQuestion = await appQuestionService.findQuestionById(userAnswer.QuestionId);
                var appAnswer = await appAnswerService.findAnswerById(userAnswer.AnswerId);

                if (appQuestion != null && appAnswer != null)
                {
                    prompt += $"- {appQuestion.QuestionText}: {appAnswer.AnswerText}\n";
                }
            }

            prompt += "Prove un horario semanal de entreno incluyendo ejercicios, series, repeticiones y días de descanso";

            return prompt;
        }


        public async Task<AIMessage?> GenerateRoutineFromAI(AppUser? user, string prompt)
        {

            if (appUserService == null || coachModelTypeService == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(prompt) || user == null)
            {
                return null;
            }

            CoachModelType? model = await coachModelTypeService.GetCoachModelTypeById(user.CoachModelTypeId.ToString());

            if (model == null)
            {
                return null;
            }

            var urlAddress = AppSettings.BaseAddress + "/chat/" + model + "/memoryId?=" + "&message=" + prompt;
            var response = await AppSettings._HttpClient.GetAsync(urlAddress);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseData))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<AIMessage>(responseData);
                }
            }
            return null;
        }
    }

}
