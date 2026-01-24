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
        builder.Services.AddSingleton<QuestionnaireService>();
        builder.Services.AddSingleton<AIRoutineService>();
        builder.Services.AddSingleton<QuestionnaireService>();


        // Register WebService with necessary parameters from AppSettings
        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();

        builder.Services.AddSingleton<WebService>(sp =>
            new WebService(AppSettings._HttpClient, AppSettings.ApiGatewayBaseUrl, AppSettings.RefreshTokenEndpoint)
        );

        return builder.Build();
    }
}
