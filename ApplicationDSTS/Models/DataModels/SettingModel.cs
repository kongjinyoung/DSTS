using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationDSTS.Models.DataModels
{
    class SettingModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public int HThreshold { get; set; } // Operation High Threshold
        public int LThreshold { get; set; } // Operation Low Threshold
    }

    public class CommonDataModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public string IpAddress { get; set; }
        public int Port { get; set; }
        public int Protocol { get; set; } // [0]HEX [1]ASCII
        public string Path { get; set; } // save directory path

    }
}
