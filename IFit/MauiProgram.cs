using IFit.Services;
using Microsoft.Extensions.Logging;

namespace IFit;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>() 
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });


        // Register services
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<AppUserService>();
        builder.Services.AddSingleton<AuthenticationService>();
        builder.Services.AddSingleton<CoachModelTypeService>();
        builder.Services.AddSingleton<ExperienceLevelService>();
        builder.Services.AddSingleton<AppQuestionnaireService>();
        builder.Services.AddSingleton<AppQuestionService>();
        builder.Services.AddSingleton<AnswerService>();
        builder.Services.AddSingleton<AppUserQuestionnaireService>();
        builder.Services.AddSingleton<AppUserAnswerService>();

        return builder.Build();
    }
}
