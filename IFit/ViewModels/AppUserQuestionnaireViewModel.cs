using IFit.Helper;
using IFit.Models;
using IFit.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IFit.ViewModels
{
    public class AppUserQuestionnaireViewModel : INotifyPropertyChanged
    {
        // Services
        private DatabaseService? databaseService = App.GetService<DatabaseService>();
        private AppUserService? appUserService = App.GetService<AppUserService>();
        private AppQuestionService? questionService = App.GetService<AppQuestionService>();
        private AnswerService? answerService = App.GetService<AnswerService>();
        private AppUserQuestionnaireService? appUserQuestionnaireService = App.GetService<AppUserQuestionnaireService>();
        private AppUserAnswerService? appUserAnswerService = App.GetService<AppUserAnswerService>();

        #region Questionnaire and Questions
        private AppQuestionnaire appQuestionnaire = new AppQuestionnaire();
        public AppQuestionnaire AppQuestionnaire
        {
            get => appQuestionnaire;
            set
            {
                appQuestionnaire = value;
            }
        }

        private List<AppQuestion> appQuestions = new List<AppQuestion>();
        public List<AppQuestion> AppQuestions
        {
            get => appQuestions;
            set
            {
                appQuestions = value;
            }
        }

        // User question response
        private int currentQuestionIndex = 0;

        private String questionTitle = string.Empty;
        public String QuestionTitle
        {
            get => questionTitle;
            set
            {
                questionTitle = value;
                OnPropertyChanged(nameof(QuestionTitle));
            }
        }

        private List<AppAnswer> questionAnswers = new List<AppAnswer>();
        public List<AppAnswer> QuestionAnswers
        {
            get => questionAnswers;
            set
            {
                questionAnswers = value;
                OnPropertyChanged(nameof(QuestionAnswers));
            }
        }

        private AppAnswer? selectedAnswer;
        public AppAnswer? SelectedAnswer
        {
            get => selectedAnswer;
            set
            {
                selectedAnswer = value;
                OnPropertyChanged(nameof(SelectedAnswer));
                OnSelectedAnswer();
            }
        }

        private List<AppUserAnswer> userAnswers = new List<AppUserAnswer>();
        private void OnSelectedAnswer()
        {
            userAnswers.Add(new AppUserAnswer
            {
                QuestionId = AppQuestions[currentQuestionIndex].Id,
                AnswerId = SelectedAnswer?.Id ?? 0
            });

            currentQuestionIndex++;
              
            // Update to next question
            if (currentQuestionIndex < AppQuestions.Count)
            {
                QuestionTitle = AppQuestions[currentQuestionIndex].QuestionText;
                QuestionAnswers = AppQuestions[currentQuestionIndex].Answers ?? new List<AppAnswer>();
            }
            else
            {
                // All questions answered, save answers
                _ = SaveUserAnswersAsync();
            }
        }

        private async Task<Boolean> SaveUserAnswersAsync()
        {
            if (databaseService == null || appUserQuestionnaireService == null || appUserAnswerService == null)
            {
                await ErrorHandler.HandleErrorAsync("Services are not initialized.", "//ErrorView");
                return false;
            }


            AppUser? user = await databaseService.GetCurrentUserAsync();
            if (user == null)
            {
                await ErrorHandler.HandleErrorAsync("No user found in the database.", "//ErrorView");
                return false;
            }

            // Save questionnaire for user
            var result = await appUserQuestionnaireService.SaveAppUserQuestionnaire(user.Id, appQuestionnaire.Id);
            if (result == null)
            {
                await ErrorHandler.HandleErrorAsync("Failed to set questionnaire for user.", "//ErrorView",
                    "Error",
                    "No se pudo establecer el cuestionario para el usuario. Por favor, inténtelo más tarde.");
                return false;
            }

            // Save user answers
            foreach (var userAnswer in userAnswers)
            {
                var answerResult = await appUserAnswerService.SaveUserAnswer(user.Id, userAnswer.QuestionId, AppQuestionnaire.Id, userAnswer.AnswerId);
                if (answerResult == null)
                {
                    await ErrorHandler.HandleErrorAsync("Failed to save user answer.", "//ErrorView",
                        "Error",
                        "No se pudo guardar la respuesta del usuario. Por favor, inténtelo más tarde.");
                    return false;
                }
            }

            return true;
        }

        #endregion

        public AppUserQuestionnaireViewModel()
        {
            _ = LoadDataForQuestionnaire();
        }


        # region LoadData
        private async Task LoadDataForQuestionnaire()
        {
            await LoadQuestionnaireForCurrentUserAsync();
            await LoadQuestionsForQuestionnaireAsync();
            await LoadAnswersForQuestionsAsync();

            // Init Data for first question
            if (AppQuestions != null && AppQuestions.Count > 0)
            {
                QuestionTitle = AppQuestions[currentQuestionIndex].QuestionText;
                QuestionAnswers = AppQuestions[currentQuestionIndex].Answers ?? new List<AppAnswer>();
            }
        }

        public async Task<bool> LoadQuestionnaireForCurrentUserAsync()
        {
            if (databaseService == null || appUserService == null || questionService == null)
            {
                await ErrorHandler.HandleErrorAsync("Services are not initialized.", "//ErrorView");
                return false;
            }

            AppUser? user = await databaseService.GetCurrentUserAsync();
            if (user == null)
            {
                await ErrorHandler.HandleErrorAsync("No user found in the database.", "//ErrorView");
                return false;
            }

            AppQuestionnaire? questionnaire = await appUserService.GetQuestionnaireForUserAsync(user);
            if (questionnaire == null)
            {
                await ErrorHandler.HandleErrorAsync("No questionnaire found for the current user.", "//ErrorView",
                    "Error",
                    "No se encontró ningún cuestionario para el usuario actual. Por favor, póngase en contacto con el soporte.");
                return false;
            }

            AppQuestionnaire = questionnaire;
            return true;
        }

        public async Task<bool> LoadQuestionsForQuestionnaireAsync()
        {
            if (databaseService == null || appUserService == null || questionService == null)
            {
                await ErrorHandler.HandleErrorAsync("Services are not initialized.", "//ErrorView");
                return false;
            }

            if (AppQuestionnaire == null || AppQuestionnaire.Id <= 0)
            {
                await ErrorHandler.HandleErrorAsync("Questionnaire is not set or invalid.", "//ErrorView");
                return false;
            }

            List<AppQuestion>? questions = await questionService.findQuestionsByQuestionnaireId(AppQuestionnaire.Id);
            if (questions == null || questions.Count == 0)
            {
                await ErrorHandler.HandleErrorAsync("No questions found for the questionnaire.", "//ErrorView",
                    "Error",
                    "No se encontraron preguntas para el cuestionario. Por favor, póngase en contacto con el soporte.");
                return false;
            }

            AppQuestions = questions;
            return true;
        }

        public async Task<bool> LoadAnswersForQuestionsAsync()
        {
            if (databaseService == null || appUserService == null || questionService == null || answerService == null)
            {
                await ErrorHandler.HandleErrorAsync("Services are not initialized.", "//ErrorView");
                return false;
            }

            if (AppQuestions == null || AppQuestions.Count == 0)
            {
                await ErrorHandler.HandleErrorAsync("Questions are not loaded.", "//ErrorView");
                return false;
            }

            foreach (var question in AppQuestions)
            {
                List<AppAnswer>? answers = await answerService.findAnswersByQuestionId(question.Id);
                if (answers == null || answers.Count == 0)
                {
                    await ErrorHandler.HandleErrorAsync($"No answers found for question ID {question.Id}.", "//ErrorView",
                        "Error",
                        $"No se encontraron respuestas para la pregunta con ID {question.Id}. Por favor, póngase en contacto con el soporte.");
                    return false;
                }
                question.Answers = answers;
            }
            return true;
        }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
