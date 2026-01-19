using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IFit.Helper;
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

            var userAnswersIds = await appUserAnswerService.GetUserAnswersForQuestionnaireAsync(user.Id, questionnaire.QuestionnaireId);
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
            // Check services
            if (appUserService == null || coachModelTypeService == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(prompt) || user == null)
            {
                return null;
            }


            // Get Coach Model Type
            CoachModelType? model = new CoachModelType(); // TO-DO: Obtener el coach model type del usuario.

            if (model == null)
            {
                return null;
            }

            // Get Max Memory Id
            var maxMemoryId = await GetMaxMemoryId();
            if (maxMemoryId == null) {
                await ErrorHandler.HandleErrorAsync("Ups!", "No hemos podido procesar su solicitud.");
                await Shell.Current.GoToAsync("//ErrorPage");
                return null;
            }
            maxMemoryId.Id += 1;

           // Chat with AI
            return await chat(model, maxMemoryId.Id, prompt);
        }

        public async Task<AIMessage?> chat(CoachModelType model, int memoryId, string prompt)
        {
            var urlAddress = AppSettings.BaseAddress + "/chat/" + model.Name;

            var aiMessage = new AIMessage(
                MemoryId: memoryId,
                Message: prompt
            );

            var content = System.Text.Json.JsonSerializer.Serialize(aiMessage);
            HttpContent httpContent = new StringContent(content, Encoding.UTF8, "application/json");

            var response = await AppSettings._HttpClient.PostAsync(urlAddress, httpContent);
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
        #region Aux

            /** Get Max Memory Id from AI Server
             */
        private async Task<MaxMemoryId?> GetMaxMemoryId()
        {
            var urlAddress = AppSettings.BaseAddress + "/messages/max-memory-id";
            var response = await AppSettings._HttpClient.GetAsync(urlAddress);
            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(responseData))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<MaxMemoryId>(responseData);
                }
            }
            return null;
        }

        #endregion Aux
    }

}
