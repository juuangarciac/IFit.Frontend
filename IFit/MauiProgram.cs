using IFit.Services;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

namespace IFit;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
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
        builder.Services.AddSingleton<TrainingService>();
        builder.Services.AddSingleton<ExerciseCatalogService>();
        builder.Services.AddSingleton<AppEmailService>();
        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
        builder.Services.AddSingleton<WebService>(sp =>
            new WebService(AppSettings._HttpClient, AppSettings.ApiGatewayBaseUrl, AppSettings.RefreshTokenEndpoint)
        );

        return builder.Build();
    }
}