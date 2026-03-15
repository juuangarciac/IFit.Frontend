using IFit.ViewModels;

namespace IFit.Views;

public partial class ChatAIView : ContentPage
{
    private ChatAIViewModel _viewModel => BindingContext as ChatAIViewModel;

    public ChatAIView()
    {
        InitializeComponent();
        BindingContext = new ChatAIViewModel();

        // Auto-scroll al añadir mensajes
        if (_viewModel != null)
        {
            _viewModel.Messages.CollectionChanged += async (s, e) =>
            {
                await Task.Delay(100);
                await ChatScrollView.ScrollToAsync(0, ChatScrollView.ContentSize.Height, true);
            };
        }
    }

    public async void BackToHome(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"///HomeView");
    }
}
