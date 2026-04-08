using IFit.Views;
using IFit.Views.Components;

namespace IFit
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Routing
            Routing.RegisterRoute("MainPage", typeof(MainPage));
            Routing.RegisterRoute("SignUpView", typeof(SignUpView));
            Routing.RegisterRoute("SignInView", typeof(SignInView));
            Routing.RegisterRoute("VerificationView", typeof(VerificationView));
            Routing.RegisterRoute("HomeView", typeof(HomeView));
            Routing.RegisterRoute("GetStartedView", typeof(GetStartedView));
            Routing.RegisterRoute("CoachModelTypeSelectionView", typeof(CoachModelTypeSelectionView));
            Routing.RegisterRoute("ExperienceLevelSelectionView", typeof(ExperienceLevelSelectionView));
            Routing.RegisterRoute("AppUserQuestionnaireView", typeof(AppUserQuestionnaireView));
            Routing.RegisterRoute("AIGenerationRoutineView", typeof(AIGenerationRoutineView));
            Routing.RegisterRoute("QuestionnaireSummaryView", typeof(QuestionnaireSummaryView));
            Routing.RegisterRoute("RoutineSummaryView", typeof(RoutineSummaryView));
            Routing.RegisterRoute("TrainingDayDetailView", typeof(TrainingDayDetailView));
            Routing.RegisterRoute("ChatAIView", typeof(ChatAIView));
            Routing.RegisterRoute("WeeklySummaryView", typeof(WeeklySummaryView));
            Routing.RegisterRoute("PlanView", typeof(PlanView));
            Routing.RegisterRoute("PlanSummaryView", typeof (PlanView));
            Routing.RegisterRoute("ProfileView", typeof(ProfileView));
        }
    }
}
