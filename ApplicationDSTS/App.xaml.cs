using ApplicationDSTS.Models.Clients;
using ApplicationDSTS.Models.DataModels;
using ApplicationDSTS.Models.Managers;
using ApplicationDSTS.Views;
using DevExpress.Xpf.Core;
using NLog;
using SciChart.Charting.Visuals;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ApplicationDSTS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // views
        public MainView MainView { get; private set; }
        public HistoryView HistoryView { get; private set; }

        
        // data managers
        public DbManager DbManager { get; private set; }
        public StatusManager StatusManager { get; private set; }
        public ConvertManager ConvertManager { get; private set; }

        // data models
        public SystemSetDataModel SystemSetDataModel { get; set; }
        public CommonSetDataModel CommonSetDataModel { get; set; }
        public ControlSetDataModel ControlSetDataModel { get; set; }
        public ConfigureSetDataModel ConfigureSetDataModel { get; set; }
        public TraceSetDataModel TraceSetDataModel { get; set; }
        public ReferenceSetDataModel ReferenceSetDataModel { get; set; }
        public OperationSetDataModel OperationSetDataModel { get; set; }
        public AsyncObservableCollection<HistoryEventData> HistoryEventData { get; set; }
        public AsyncObservableCollection<HistoryChartData> HistoryChartData { get; set; }

        // App variable
        public EthernetClient Client { get; private set; }

        public readonly Logger nlog = LogManager.GetLogger("");
        public int DeviceRange { get; set; }
        public bool DeviceConnection { get; set; } // [T] system start || [F] system stop
        public bool DeviceStatus { get; set; } // [T] trace || [F] reference/operation


        public App()
        {
            // kjy@up-tec.co.kr 라이센스
            SciChartSurface.SetRuntimeLicenseKey("oQqSSSFezvsiR7L83SsiuyoBu8UASGj8vmo0ktqoUc61utvVK/jS1fATkXCYmwrEko6hI54S1V/vnMOYCFDWxeLkwbQADkghe8//G+PUXWAdOR8AccZLWUVEIULeejsGsnFV/dcOmPvKLpzp4YeplOQuVUpZCSMZ24nO5BNku7ot2OeGrsUW2yMrmyTh7ksheKLoboRE1fw0bJ69njYSj6U+le6qDF7TXAueZs4Q6FrynbPblBtSNJTIlaNcVwWAiI/qwSor7Xw1f1+kB7nO5js7X2OzV4aPDopITSUL1T+EMdBp06RNdRG1ToNoSvmRMnCLGPDxVBj6fCDC68mhuntz0B6YoKMK/60fFYS5NxbrMcuxGtYg0cR19zH5OHTWW9hoY2OecYIPklWUBDn9eZSALf5czy6robZAle/nEOmCQ5zY7GVCJfnE7yHeD2Aj1t/jUm+1MiiAojZO+E+Kl3LTZjFW7369GJG4VrIJfA==");
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            AppInitializer();

            base.OnStartup(e);
        }

        private void AppInitializer()
        {
            InitManagers();

            InitDataModels();

            InitViews();

            EthernetClientSet();

            StatusManager.CurrentCommStatus = StatusManager.CommStatus.None;
        }
        public void InitDataModels()
        {
            SystemSetDataModel = new SystemSetDataModel();
            SystemSetDataModel = DbManager.LoadSystemData();

            CommonSetDataModel = new CommonSetDataModel();
            CommonSetDataModel = DbManager.LoadCommonData();

            ControlSetDataModel = new ControlSetDataModel();
            ControlSetDataModel = DbManager.LoadControlData();

            ConfigureSetDataModel = new ConfigureSetDataModel();
            ConfigureSetDataModel = DbManager.LoadConfigureData();

            TraceSetDataModel = new TraceSetDataModel();
            TraceSetDataModel = DbManager.LoadTraceData();

            ReferenceSetDataModel = new ReferenceSetDataModel();
            ReferenceSetDataModel = DbManager.LoadReferenceData();

            OperationSetDataModel = new OperationSetDataModel();

            HistoryEventData = new AsyncObservableCollection<HistoryEventData>();
            HistoryChartData = new AsyncObservableCollection<HistoryChartData>();

            EthernetClientSet();
        } 
        private void InitManagers()
        {
            StatusManager = new StatusManager();
            StatusManager.Init();

            DbManager = new DbManager();

            ConvertManager = new ConvertManager();
        }        
        private void InitViews()
        {
            MainView = new MainView();

            HistoryView = new HistoryView();
        }

        // Super Socket Client
        private void EthernetClientSet()
        {
            // DSS Communication
            Client = new EthernetClient();
            Client.ReceiveBufferSize = 262144; // Client 최대 바이트 수

            Client.Connected += (s, e) =>
            {
                StatusManager.CurrentNetworkStatus = StatusManager.NetworkStatus.Connected;
                DeviceConnection = true;
                nlog.Info($"connected server. (serv) "); // + {CommonDataModel.IpAddress}
                UpdatingVariableConnectSignal(1000);
            };

            Client.Closed += (s, e) =>
            {
                StatusManager.CurrentNetworkStatus = StatusManager.NetworkStatus.Disconnected;
                DeviceConnection = false;
                nlog.Error($"disconnected server. (serv) {CommonSetDataModel.IpAddress}");
                if (StatusManager.KeepConnection)
                {
                    Thread.Sleep(3000);
                    TryConnection();
                }

                MessageBox.Show("DSS에서 튕김" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "DSS System Message", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            Client.Error += (s, e) =>
            {
                StatusManager.CurrentNetworkStatus = StatusManager.NetworkStatus.Error;
                DeviceConnection = false;

                if (StatusManager.KeepConnection)
                {
                    Thread.Sleep(3000);
                    TryConnection();
                }
            };

            TryConnection();
        }
        private void TryConnection()
        {
            if (!StatusManager.KeepConnection)
            {
                StatusManager.CurrentNetworkStatus = StatusManager.NetworkStatus.Error;
                return;
            }
            if (Client.IsConnected)
            {
                return;
            }
            Task.Factory.StartNew(() =>
            {
                bool pingable = false;
                string ip = CommonSetDataModel.IpAddress;
                int port = CommonSetDataModel.Port;
                nlog.Info($"connecting server. (serv) {CommonSetDataModel.IpAddress}");
                while (true)
                {
                    StatusManager.CurrentNetworkStatus = StatusManager.NetworkStatus.TryConnect;
                    pingable = PingToCanConnect(pingable, ip, 5000);
                    if (pingable)
                    {
                        IPEndPoint ep = new IPEndPoint(IPAddress.Parse(ip), port);
                        Client.ConnectAsync(ep);
                        return;
                    }
                    else
                    {
                        StatusManager.Network = ApplicationDSTS.Properties.Resources.App_StatusNetwork_Error;
                        Thread.Sleep(3000);
                    }
                }
            });
        }
        // Pint To Socket
        private bool PingToCanConnect(bool pingable, string ip, int port)
        {
            using (Ping ping = new Ping())
            {
                try
                {
                    PingReply reply = ping.Send(ip, port);
                    pingable = reply.Status == IPStatus.Success;
                }
                catch (PingException e)
                {
                    System.Diagnostics.Debug.WriteLine("==============================================================================================");
                    System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString());
                    System.Diagnostics.Debug.WriteLine("Message : " + e.Message);
                    System.Diagnostics.Debug.WriteLine("Trace   : " + e.StackTrace + e.StackTrace.Substring(e.StackTrace.LastIndexOf(' ')));
                    System.Diagnostics.Debug.WriteLine("==============================================================================================");
                }
                return pingable;
            }
        }

        // 연결
        public async void UpdatingVariableConnectSignal(int time)
        {
            StatusManager.CurrentCommStatus = StatusManager.CommStatus.Connected;
            await Task.Delay(time);
            StatusManager.CurrentCommStatus = StatusManager.CommStatus.None;
        }
        // 저장
        public async void UpdatingVariableSavedSignal(int time)
        {
            StatusManager.CurrentCommStatus = StatusManager.CommStatus.Saved;
            await Task.Delay(time);
            StatusManager.CurrentCommStatus = StatusManager.CommStatus.None;
        }
        // 업데이트
        public async void UpdatingVariableUpdatedSignal(int time)
        {
            StatusManager.CurrentCommStatus = StatusManager.CommStatus.Updated;
            await Task.Delay(time);
            StatusManager.CurrentCommStatus = StatusManager.CommStatus.None;
        }
        // 업로드
        public async void UpdatingVariableUploadSignal(int time)
        {
            StatusManager.CurrentCommStatus = StatusManager.CommStatus.Uploaded;
            await Task.Delay(time);
            StatusManager.CurrentCommStatus = StatusManager.CommStatus.None;
        }
        // 에러코드  - 리셋
        public async void UpdatingVariableResetErrorCodeSignal(int time)
        {
            StatusManager.CurrentCommStatus = StatusManager.CommStatus.ErrorCode;
            await Task.Delay(time);
            StatusManager.CurrentCommStatus = StatusManager.CommStatus.None;
        }
        private readonly string myip = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            // 애플리케이션 종료 시 실행되는 코드
            nlog.Info($"application exit. (main) {myip}");
        }
    }
}
