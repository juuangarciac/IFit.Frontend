using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace IFit.ViewModels;

public partial class GetStartedViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    #region Constructor
    public GetStartedViewModel()
	{
		LoadUserName();
	}

    #endregion

    #region Commands

    [RelayCommand]
    public async Task GetStartedAsync()
    {
        await Shell.Current.GoToAsync("ExperienceLevelSelectionView");
    }

    #endregion

    #region Methods
    private void LoadUserName()
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

    #endregion
}