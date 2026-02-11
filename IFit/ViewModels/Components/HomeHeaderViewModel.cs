using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Plugin.Maui.Calendar.Controls;
using Plugin.Maui.Calendar.Enums;

namespace IFit.ViewModels.Templates;

public class HomeHeaderViewModel : INotifyPropertyChanged
{
    private const WeekLayout defaultCalendarLayout = WeekLayout.Week;

    private WeekLayout selectedLayout;

    public WeekLayout SelectedLayout
    {
        get { return selectedLayout; }
        set
        {
            if (selectedLayout != value)
            {
                selectedLayout = value;
                OnPropertyChanged(nameof(SelectedLayout));
            }
        }
    }

    public HomeHeaderViewModel() {
        SelectedLayout = defaultCalendarLayout;
    }

    #region ICommand

    public ICommand ExpandCalendarSelected { get { return new Command(OnExpandCalendarSelected); } }

    private void OnExpandCalendarSelected()
    {
        if(selectedLayout == WeekLayout.Week)
        {
            SelectedLayout = WeekLayout.Month;
            return;
        }

        SelectedLayout = WeekLayout.Week;
    }

    #endregion

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}