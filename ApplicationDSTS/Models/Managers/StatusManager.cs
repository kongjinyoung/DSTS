using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ApplicationDSTS.Models.Managers
{
    public class StatusManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #region 변수 정의
        // 시스템 동작
        public enum SystemStatus
        {
            Start,
            Stop,
        }
        // 접속 상태
        public enum NetworkStatus
        {
            None,
            TryConnect, // 장비
            Connected,
            Disconnected,
            Error,
        }
        // 상태 이벤트 메세지(1초유지)
        public enum CommStatus
        {
            None,
            Downloading,
            DownloadError,
            Downloaded,
            Connected,
            Connecting,
            UserConnecting,
            DisConnecting,
            UserDisConnecting,
            Updated,
            UpdateError,
            Uploaded,
            Uploading,
            UploadError,
            TimeSync,
            TimeSyncComplete,
            TimeSyncError,
            Timeout,
            Saved,
            SavedError,
            ErrorCode,
        }
        #endregion

        public SystemStatus CurrentSystemStatus { get; set; } // System Status
        public CommStatus CurrentCommStatus { get; set; } // Common Status
        public NetworkStatus CurrentNetworkStatus { get; set; } // DSTS Status

        #region < Use Variable >
        /// <summary>
        /// Public variable
        /// </summary>
        public string Network { get; set; } // DSTS 정보
        public bool KeepConnection { get; set; } = true;
        public string CommState { get; set; } // HMI 상태
        public string SystemState { get; set; } // 시스템 상태
        public string CurrentTime { get; set; } // 현재시간

        /// <summary>
        /// Private variable
        /// </summary>
        private DispatcherTimer dt_timer; // 현재시간 타이머 
        private int NetworkErrorCounter = 0;

        #endregion

        public void Init() // 초기 실행 함수
        {
            dt_timer = new DispatcherTimer();
            dt_timer.Interval = new TimeSpan(0, 0, 1);  //1초간격 동작
            dt_timer.Tick += (s, e) =>
            {
                CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            };
            this.dt_timer.Start();

            this.PropertyChanged += StatusManager_PropertyChanged;
        }
        private void StatusManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentSystemStatus")
            {
                UpdateSystemStatusString();
            }
            if (e.PropertyName == "CurrentCommStatus")
            {
                UpdateCommStatus();
            }

            if (e.PropertyName == "CurrentNetworkStatus")
            {
                UpdateDASNetworkStatus();
            }
        }
        private void UpdateSystemStatusString()     // 시스템 동작 관련 상태 메세지
        {
            switch (this.CurrentSystemStatus)
            {
                case SystemStatus.Start:
                    this.SystemState = Properties.Resources.App_Status_System_Start;
                    break;

                case SystemStatus.Stop:
                    this.SystemState = Properties.Resources.App_Status_System_Stop;
                    break;
            }
        }
        private void UpdateCommStatus()        // 통신 관련 상태 메세지
        {
            switch (this.CurrentCommStatus)
            {
                case CommStatus.None:
                    this.CommState = Properties.Resources.App_Status_Na;
                    break;
                case CommStatus.Downloading:
                    this.CommState = Properties.Resources.App_Status_Downloading;
                    break;
                case CommStatus.DownloadError:
                    this.CommState = Properties.Resources.App_Status_Download_Error;
                    break;
                case CommStatus.Downloaded:
                    this.CommState = Properties.Resources.App_Status_Downloaded;
                    break;
                case CommStatus.Connected:
                    this.CommState = Properties.Resources.App_Status_Connected;
                    break;
                case CommStatus.Connecting:
                    this.CommState = Properties.Resources.App_Status_Connected;
                    break;
                case CommStatus.DisConnecting:
                    this.CommState = Properties.Resources.App_Status_Connected;
                    break;
                case CommStatus.Updated:
                    this.CommState = Properties.Resources.App_Status_Updated;
                    break;
                case CommStatus.UpdateError:
                    this.CommState = Properties.Resources.App_Status_Update_Error;
                    break;
                case CommStatus.Uploaded:
                    this.CommState = Properties.Resources.App_Status_Uploaded;
                    break;
                case CommStatus.Uploading:
                    this.CommState = Properties.Resources.App_Status_Uploading;
                    break;
                case CommStatus.UploadError:
                    this.CommState = Properties.Resources.App_Status_Upload_Error;
                    break;
                case CommStatus.TimeSync:
                    this.CommState = Properties.Resources.App_Status_TimeSync;
                    break;
                case CommStatus.TimeSyncComplete:
                    this.CommState = Properties.Resources.App_Status_TimeSyncComplete;
                    break;
                case CommStatus.TimeSyncError:
                    this.CommState = Properties.Resources.App_Status_TimeSyncError;
                    break;
                case CommStatus.Timeout:
                    this.CommState = Properties.Resources.App_Status_Timeout;
                    break;
                case CommStatus.Saved:
                    this.CommState = Properties.Resources.App_Status_Saved;
                    break;
                case CommStatus.SavedError:
                    this.CommState = Properties.Resources.App_Status_SavedError;
                    break;
            }
        }
        private void UpdateDASNetworkStatus()    // DSTS 관련 상태 메세지
        {
            switch (this.CurrentNetworkStatus)
            {
                case NetworkStatus.None:
                    this.Network = "N/A";
                    break;
                case NetworkStatus.TryConnect:
                    this.Network = Properties.Resources.App_StatusNetwork_TryConnect;
                    break;
                case NetworkStatus.Connected:
                    this.Network = Properties.Resources.App_StatusNetwork_Connected;
                    break;
                case NetworkStatus.Disconnected:
                    this.Network = Properties.Resources.App_StatusNetwork_Disconnected;
                    this.NetworkErrorCounter++;
                    break;
                case NetworkStatus.Error:
                    this.Network = Properties.Resources.App_StatusNetwork_Error;
                    this.NetworkErrorCounter++;
                    break;
            }
        }
    }
}
