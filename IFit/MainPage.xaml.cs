namespace IFit
{
    public partial class MainPage : ContentPage
    { 
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnSignUpClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///SignUpView");
        }
        private async void OnSignInClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///SignInView");
        }
    }

}
