using CommunityToolkit.Mvvm.ComponentModel;
using IFit.Models.Dtos.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFit.ViewModels.Components
{
    [QueryProperty(nameof(TrainingDayDto), "TrainingDayDto")]
    public partial class PlanDayDetailViewModel : ObservableObject
    {
        #region Properties

        [ObservableProperty]
        public partial TrainingDayDto TrainingDayDto { get; set; }

        #endregion

        #region Constructor

        public PlanDayDetailViewModel() { }

        public async Task AppearingAsync()
        {

        }

        #endregion
    }
}
