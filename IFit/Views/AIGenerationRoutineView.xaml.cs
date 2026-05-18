namespace IFit.Views;

public partial class AIGenerationRoutineView : ContentPage
{
    public AIGenerationRoutineView()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            var innerException = ex.InnerException;
            var message = innerException?.Message ?? ex.Message;
            var stackTrace = innerException?.StackTrace ?? ex.StackTrace;

            System.Diagnostics.Debug.WriteLine($"Error: {message}");
            System.Diagnostics.Debug.WriteLine($"StackTrace: {stackTrace}");
            throw;
        
        }
    }

    public async void onCancelClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    public async void onCloseClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("///MainPage");
}