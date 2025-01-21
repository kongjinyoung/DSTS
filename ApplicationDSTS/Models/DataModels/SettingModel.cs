using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ApplicationDSTS.Models.Managers;

namespace ApplicationDSTS.Models.DataModels
{
    /// <summary>
    /// System Data Model
    /// </summary>
    public class SystemSetDataModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public int HThreshold { get; set; } // Operation High Threshold
        public int LThreshold { get; set; } // Operation Low Threshold
    }
    /// <summary>
    /// Common Data Model
    /// </summary>
    public class CommonSetDataModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public string Protocol { get; set; } // [0]HEX [1]ASCII
        public string Path { get; set; } // save directory path

    }
    /// <summary>
    /// Control Data Model
    /// </summary>
    public class ControlSetDataModel : INotifyPropertyChanged
    {
        App app = Application.Current as App;
        StringBuilder sb = new StringBuilder();
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        // 광원 상태
        private bool lightStatus { get; set; }
        public bool LightStatus // [F] off, [T] on
        {
            get => lightStatus;
            set
            {
                lightStatus = value;
                if (app.DeviceConnection && lightStatus) // on
                {
                    sb.Append($"﻿CONT:SOUR:EN 1\r\n");
                }
                else if(app.DeviceConnection && !lightStatus) // off
                {
                    sb.Append($"﻿CONT:SOUR:EN 0\r\n");
                }

                app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), MainModel.ethernetClient);

                app.DbManager.UpdateControlData(app.ControlSetDataModel, 0); // db 저장
                OnPropertyChanged(nameof(LightStatus));
                app.UpdatingVariableUpdatedSignal(1000);
            }
        }
        // 펄스 상태
        private bool pulseStatus { get; set; }
        public bool PulseStatus // [F] off, [T] on
        {
            get => pulseStatus;
            set
            {
                pulseStatus = value;

                if (app.DeviceConnection && pulseStatus) // on
                {
                    sb.Append($"﻿CONT:﻿PULS:EN 1\r\n");
                    app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), MainModel.ethernetClient);
                }
                else if (app.DeviceConnection && !pulseStatus) // off
                {
                    sb.Append($"﻿CONT:﻿PULS:EN 0\r\n");
                    app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), MainModel.ethernetClient);
                }

                app.DbManager.UpdateControlData(app.ControlSetDataModel, 1); // db 저장
                OnPropertyChanged(nameof(PulseStatus));
                app.UpdatingVariableUpdatedSignal(1000);
            }
        }
    }

    /// <summary>
    /// Configure Data Model
    /// </summary>
    public class ConfigureSetDataModel : INotifyPropertyChanged
    {
        App app = Application.Current as App;
        StringBuilder sb = new StringBuilder();
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        private float _spatRes { get; set; } // 공간 분해능
        public float SpatRes
        {
            get => _spatRes;
            set
            {
                if (value == 1 || value == 2)
                {
                    if (app.DeviceConnection)
                    {
                        _spatRes = value;
                        sb.Append($"CONF:ACQ:SRES {value}\r\n");
                        app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), MainModel.ethernetClient);

                        OnPropertyChanged(nameof(SpatRes));
                        app.UpdatingVariableUpdatedSignal(1000);
                    }
                }
            }
        }
        private float _sampInterval { get; set; } // 공간 샘플링 간격
        public float SampInterval
        {
            get => _sampInterval;
            set
            {
                if(value != 0.25 || value != 0.5)
                {
                    value = _sampInterval;
                }
                else
                {
                    if (app.DeviceConnection)
                    {
                        _sampInterval = value;
                        app.DeviceRange = Convert.ToInt32(value / app.ConfigureSetDataModel.SampInterval);

                        sb.Append($"CONF:ACQ:SPAT {value}\r\n");
                        app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), MainModel.ethernetClient);

                        OnPropertyChanged(nameof(SampInterval));
                        app.UpdatingVariableUpdatedSignal(1000);
                    }
                }
            }
        }
        private float _range { get; set; } // 계측 거리
        public float Range
        {
            get => _range;
            set
            {
                if (value < 100 || value > 60000)
                {
                    value = _range;
                }
                else
                {
                    if (app.DeviceConnection)
                    {
                        _range = value;
                        app.DeviceRange = Convert.ToInt32(value / app.ConfigureSetDataModel.SampInterval);

                        sb.Append($"CONF:ACQ:RANG {value}\r\n");
                        app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), MainModel.ethernetClient);

                        OnPropertyChanged(nameof(Range));
                        app.UpdatingVariableUpdatedSignal(1000);
                    }
                }
            }
        }
        public int IntegNum { get; set; } // 적분 횟수
        private float _initFreq { get; set; } // 시작 탐사 주파수
        public float InitFreq
        {
            get => _initFreq;
            set
            {
                if (value < 10600 || value > 11100)
                {
                    value = _initFreq;
                }
                else
                {
                    if (app.DeviceConnection)
                    {
                        _initFreq = value;
                        sb.Append($"CONF:SWEE:INIT {value}\r\n");
                        app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), MainModel.ethernetClient);

                        OnPropertyChanged(nameof(InitFreq));
                        app.UpdatingVariableUpdatedSignal(1000);
                    }
                }
            }
        }
        private float _sweepInterval { get; set; } // 탐사 주파수 간격
        public float SweepInterval
        {
            get => _sweepInterval;
            set
            {
                if (value < 1 || value > 20)
                {
                    value = _sweepInterval;
                }
                else
                {
                    if (app.DeviceConnection)
                    {
                        _sweepInterval = value;
                        sb.Append($"CONF:SWEE:﻿DEV {value}\r\n");
                        app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), MainModel.ethernetClient);
                        
                        OnPropertyChanged(nameof(SweepInterval));
                        app.UpdatingVariableUpdatedSignal(1000);
                    }
                }
            }
        }
        private float _sweepNum { get; set; } // 탐사 주파수 스윕 횟수
        public float SweepNum
        {
            get => _sweepNum;
            set
            {
                if (value < 1 || value > 501)
                {
                    value = _sweepNum;
                }
                else
                {
                    if (app.DeviceConnection)
                    {
                        _sweepNum = value;
                        sb.Append($"CONF:SWEE:COUN {value}\r\n");
                        app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), MainModel.ethernetClient);
                        
                        OnPropertyChanged(nameof(SweepNum));
                        app.UpdatingVariableUpdatedSignal(1000);
                    }
                }
            }
        }
    }
    /// <summary>
    /// Trace Data Model
    /// </summary>
    public class TraceSetDataModel : INotifyPropertyChanged
    {
        App app = Application.Current as App;
        StringBuilder sb = new StringBuilder();
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        private float _edfa1 { get; set; } // 펄스 증폭기 전류
        public float Edfa1
        {
            get => _edfa1;
            set
            {
                if (value < 0 || value > 600)
                {
                    value = _edfa1;
                }
                else
                {
                    if (app.DeviceConnection)
                    {
                        _edfa1 = value;
                        sb.Append($"CONT:EDFA:BIAS 1 {value}\r\n");
                        app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), MainModel.ethernetClient);

                        OnPropertyChanged(nameof(Edfa1));
                        app.UpdatingVariableUpdatedSignal(1000);
                    }
                }
            }
        }
        private float _edfa2 { get; set; } // 신호 증폭기 전류
        public float Edfa2
        {
            get => _edfa2;
            set
            {
                if (value < 0 || value > 100)
                {
                    value = _edfa2;
                }
                else
                {
                    if (app.DeviceConnection)
                    {
                        _edfa2 = value;
                        sb.Append($"CONT:EDFA:BIAS 2 {value}\r\n");
                        app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), MainModel.ethernetClient);

                        OnPropertyChanged(nameof(Edfa2));
                        app.UpdatingVariableUpdatedSignal(1000);
                    }
                }
            }
        }
        private float _aBias1 { get; set; } // 감쇄기 바이어스 1
        public float ABias1
        {
            get => _aBias1;
            set
            {
                if (value < 0 || value > 4095)
                {
                    value = _aBias1;
                }
                else
                {
                    if (app.DeviceConnection)
                    {
                        _aBias1 = value;
                        sb.Append($"CONT:VOA:BIAS 1 {value}\r\n");
                        app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), MainModel.ethernetClient);

                        OnPropertyChanged(nameof(ABias1));
                        app.UpdatingVariableUpdatedSignal(1000);
                    }
                }
            }
        }
        private float _aBias2 { get; set; } // 감쇄기 바이어스 2
        public float ABias2
        {
            get => _aBias2;
            set
            {
                if (value < 0 || value > 4095)
                {
                    value = _aBias2;
                }
                else
                {
                    if (app.DeviceConnection)
                    {
                        _aBias2 = value;
                        sb.Append($"CONT:VOA:BIAS 2 {value}\r\n");
                        app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), MainModel.ethernetClient);

                        OnPropertyChanged(nameof(ABias2));
                        app.UpdatingVariableUpdatedSignal(1000);
                    }
                }
            }
        }
        private float _probFreq { get; set; } // 탐사 주파수
        public float ProbFreq
        {
            get => _probFreq;
            set
            {
                if (value < 10600 || value > 11100)
                {
                    value = _probFreq;
                }
                else
                {
                    if (app.DeviceConnection)
                    {
                        _probFreq = value;
                        sb.Append($"CONT:EOM:FREQ {value}\r\n");
                        app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), MainModel.ethernetClient);

                        OnPropertyChanged(nameof(ProbFreq));
                        app.UpdatingVariableUpdatedSignal(1000);
                    }

                }
            }
        }
        private float _eomBias { get; set; } // 변조 바이어스
        public float EomBias
        {
            get => _eomBias;
            set
            {
                if (value < 0 || value > 4095)
                {
                    value = _eomBias;
                }
                else
                {
                    if (app.DeviceConnection)
                    {
                        _eomBias = value;
                        sb.Append($"CONT:EOM:BIAS {value}\r\n");
                        app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), MainModel.ethernetClient);

                        OnPropertyChanged(nameof(EomBias));
                        app.UpdatingVariableUpdatedSignal(1000);
                    }
                }
            }
        }

        private bool _eomSwitch { get; set; } // 변조 전원
        public bool EomSwitch // [F] off, [T] on
        {
            get => _eomSwitch;
            set
            {
                _eomSwitch = value;
                if (app.DeviceConnection && _eomSwitch) // T:1
                {
                    sb.Append($"CONT:EOM:EN 1\r\n");
                }
                else if(app.DeviceConnection && !_eomSwitch) // F:0
                {
                    sb.Append($"CONT:EOM:EN 0\r\n");
                }

                app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), MainModel.ethernetClient);
                OnPropertyChanged(nameof(EomSwitch));
                app.UpdatingVariableUpdatedSignal(1000);
            }
        }

    }
    /// <summary>
    /// Reference Data Model
    /// </summary>
    public class ReferenceSetDataModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public int Ref_Ts { get; set; } // 온도 보상 시작 지점
        public int Ref_Td { get; set; } // 온도 보상 끝 지점
        public float Ref_Temp { get; set; } // 참조 온도
        private int ref_ds { get; set; }
        public int Ref_Ds
        {
            get => ref_ds;
            set
            {
                if (value <= 0)
                {
                    MessageBox.Show("감지영역의 시작 지점은 1이상이어야 합니다.", "Error Message", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    ref_ds = value;
                }
            }
        }
        public int Ref_Dd { get; set; } // 감지 끝 지점
        public string Ref_Value { get; set; } // byte[] 에서 hex string으로 압축된 센서 값
        public string LastUpdate { get; set; }
    }
    /// <summary>
    /// Operation Data Model
    /// </summary>
    public class OperationSetDataModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public byte[] Ref_Ds { get; set; } // 감지 영역 시작 지점
        public byte[] Ref_Dd { get; set; } // 감지 영역 끝 지점
        public string O_Value { get; set; } // Operation value
        public string O_Difference { get; set; } // Operation difference = operation value - reference value

    }
    /// <summary>
    /// History Data Model
    /// </summary>
    public class HistoryEventData
    {
        public bool IsSelected { get; set; }
        public int Index { get; set; }
        public int No { get; set; }
        public string Time { get; set; }
        public int StartLocation { get; set; }
        public int EndLocation { get; set; }
    }
    public class HistoryChartData
    {
        public byte[] Value { get; set; }
        public byte[] Difference { get; set; }
    }

    public class AsyncObservableCollection<T> : ObservableCollection<T>
    {
        private SynchronizationContext _synchronizationContext = SynchronizationContext.Current;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public AsyncObservableCollection()
        {
        }

        public AsyncObservableCollection(IEnumerable<T> list)
            : base(list)
        {
        }

        public async Task AddAsync(T item)
        {
            await _semaphore.WaitAsync();
            try
            {
                this.Add(item);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IEnumerable<T>> ToListAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return new List<T>(this);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                // Execute the CollectionChanged event on the current thread
                RaiseCollectionChanged(e);
            }
            else
            {
                // Raises the CollectionChanged event on the creator thread
                _synchronizationContext.Send(RaiseCollectionChanged, e);
            }
        }

        private void RaiseCollectionChanged(object param)
        {
            // We are in the creator thread, call the base implementation directly
            base.OnCollectionChanged((NotifyCollectionChangedEventArgs)param);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                // Execute the PropertyChanged event on the current thread
                RaisePropertyChanged(e);
            }
            else
            {
                // Raises the PropertyChanged event on the creator thread
                _synchronizationContext.Send(RaisePropertyChanged, e);
            }
        }

        private void RaisePropertyChanged(object param)
        {
            // We are in the creator thread, call the base implementation directly
            base.OnPropertyChanged((PropertyChangedEventArgs)param);
        }
    }
}
