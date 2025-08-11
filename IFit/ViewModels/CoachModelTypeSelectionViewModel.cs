using IFit.Models;
using IFit.Models.Dtos;
using IFit.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IFit.ViewModels;
public class CoachModelTypeSelectionViewModel : INotifyPropertyChanged
{

    private List<CoachModelType>? coachModelTypes = new List<CoachModelType>();
    public List<CoachModelType>? CoachModelTypes
    {
        get => coachModelTypes;
        set
        {
            if (coachModelTypes != value)
            {
                coachModelTypes = value;
                OnPropertyChanged(nameof(CoachModelTypes));
            }
        }
    }

    private CoachModelTypeService _coachModelTypeService = new CoachModelTypeService();

    public CoachModelTypeSelectionViewModel()
	{
		LoadCoachModelTypes();
    }

	public async void LoadCoachModelTypes()
	{
        List<CoachModelTypeDto>? coachModelTypeDtos = await _coachModelTypeService.GetCoachModelTypes();
        CoachModelTypes = coachModelTypeDtos?.Select(dto => new CoachModelType
        {
            Name = dto.name,
            Description = dto.description
        }).ToList();
        
        if (CoachModelTypes == null || !CoachModelTypes.Any())
        {
            if(App.Current?.MainPage != null)
            {
                await App.Current.MainPage.DisplayAlert("Error", "No se encontraron tipos de modelos de entrenador. Por favor, inténtelo más tarde.", "OK");
            }
            await Shell.Current.GoToAsync("//ErrorView");
            return;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}