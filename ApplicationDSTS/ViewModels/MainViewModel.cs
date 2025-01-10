using DevExpress.Mvvm;
using System;

namespace ApplicationDSTS.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public bool PinCommand { get; set; }

        public MainViewModel()
        {
            PinCommand = true;
        }

    }
}
