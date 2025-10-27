using System.Windows.Input;

namespace IFit.ViewModels.Templates;

public class HomeHeaderViewModel : ContentView
{

    private Style? selectedCalendarStyle;

    public Style? SelectedCalendarStyle
    {
        get { return selectedCalendarStyle; }
        set
        {
            if (selectedCalendarStyle != value)
            {
                selectedCalendarStyle = value;
                OnPropertyChanged(nameof(SelectedCalendarStyle));
            }
        }
    }

    public HomeHeaderViewModel()
	{	
        if(Application.Current != null && Application.Current.Resources != null)
        {
            SelectedCalendarStyle = Application.Current.Resources["calendar_weekly_ifit_style"] as Style;
        }
	}

    #region ICommand

    public ICommand ExpandCalendarSelected { get { return new Command(OnExpandCalendarSelected); } }

    private void OnExpandCalendarSelected()
    {
        if (Application.Current != null && Application.Current.Resources != null)
        {
            return;
        }
            // Toggle between weekly and monthly calendar styles
            if (SelectedCalendarStyle.BaseResourceKey == "calendar_weekly_ifit_style")
        {
            SelectedCalendarStyle = Application.Current.Resources["calendar_monthly_ifit_style"] as Style;
            return;
        }

        if (SelectedCalendarStyle.BaseResourceKey == "calendar_monthly_ifit_style")
        {
            SelectedCalendarStyle = Application.Current.Resources["calendar_weekly_ifit_style"] as Style;
            return;
        }
    }

    #endregion
}