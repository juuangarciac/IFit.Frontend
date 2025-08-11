using System.ComponentModel;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace IFit.ViewModels;

public class GetStartedViewModel : INotifyPropertyChanged
{
    private String _name = string.Empty;
    public String Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public GetStartedViewModel()
	{
		LoadUserName();
	}

    public void LoadUserName()
    {
        var defaultValue = "NOT_FOUND";
        Name = Preferences.Get("UserName", defaultValue);
        Console.WriteLine("UserName: " + Name + " found.");
        if (Name == defaultValue)
        {
            // Navigate to error view or handle the case where the name is not found
            Shell.Current.GoToAsync("///ErrorView");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}