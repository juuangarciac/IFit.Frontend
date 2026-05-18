namespace IFit.Views;

using IFit.ViewModels;

public partial class AppUserQuestionnaireView : ContentPage
{
    public AppUserQuestionnaireView()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is AppUserQuestionnaireViewModel vm)
            _ = vm.OnAppearingAsync();
    }

    public async void onCancelClicked(object sender, EventArgs e)
    {
        Console.WriteLine("Cancel clicked. Going back...");
        await Shell.Current.GoToAsync("///MainPage");
    }

    private void OnEntryHandlerChanged(object sender, EventArgs e)
    {
        if (sender is Entry entry)
        {
#if ANDROID
            // Eliminar la barra inferior del Entry en Android
            if (entry.Handler?.PlatformView is Android.Views.View androidView)
            {
                androidView.SetBackgroundColor(Android.Graphics.Color.Transparent);
            }
#endif
        }
    }
}
