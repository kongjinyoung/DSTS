using ApplicationDSTS.Models.Clients;
using ApplicationDSTS.Models.DataModels;
using ApplicationDSTS.Models.Managers;
using DevExpress.Mvvm;
using SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ApplicationDSTS.ViewModels
{
    public class MainViewModel : MainModel
    {
        public MainViewModel()
        {
            // common Variable
            AllowSendData = false;

            MainViewModelInit();

            TraceInit();
            ReferenceInit();
            OperationInit();
            CommandInitialize();
        }
        private void MainViewModelInit()
        {
            if (app.ReferenceSetDataModel.Ref_Ds == 0)
                app.ReferenceSetDataModel.Ref_Ds = 1;

            // request thread init
            RequestThread = new BackgroundWorker();
            RequestThread.WorkerSupportsCancellation = true;
            RequestThread.DoWork += Send_DoWork;
            RequestThread.RunWorkerCompleted += RequestThread_RunWorkerCompleted;

            // client init
            ethernetClient = app.Client;
            ethernetClient.Received += ethernetClient_Received;
            clientInfoQueue = new ConcurrentQueue<EthernetClientInfo>();
        }
        private void TraceInit()
        {
            SelectTrace = false; // trace 설정 그룹을 선택

            TraceButtonBusy = false; // trace 진행 상태 <F:사용가능 / T:진행중>
            TraceButtonEnable = true; // trace 사용가능 여부
            TraceMode = false; // trace 동작모드 <F:single / T:pair>
            TRACE_XMinEdit = 0; TRACE_XMaxEdit = app.DeviceRange; // trace chart x value range
            TRACE_YMinEdit = 0; TRACE_YMaxEdit = 17000; // trace chart y value range
            TRACE_ColorMinEdit = 0; TRACE_ColorMaxEdit = 17000; // waterfall color range
        }
        private void ReferenceInit()
        {
            ReferenceButtonBusy = false; // reference 진행 상태 <F:사용가능 / T:진행중>
            ReferenceButtonEnable = true; // reference 사용가능 여부 
        }
        private void OperationInit()
        {
            SelectOperation = false; // operation 설정 그룹 선택

            OperationButtonBusy = false; // operation 진행 상태 <F:사용가능 / T:진행중>
            OperationButtonEnable = true; // operation 사용가능 여부
            OperationCnt = 0;
            OperationStr = "";
        }
        private void CommandInitialize()
        {
            Cmd_ChangeAddress = new DelegateCommand(() => ChangeAddress());
            Cmd_SleepMode = new DelegateCommand(() => SleepMode());
            Cmd_SaveControl = new DelegateCommand(() => SaveControl());
            Cmd_SaveConfigure = new DelegateCommand(() => SaveConfigure());
            Cmd_SaveTrace = new DelegateCommand(() => SaveTrace());
            Cmd_ChangeTraceAxis = new DelegateCommand(() => ChangeTraceAxis());
            Cmd_ChangeOperationAxis = new DelegateCommand(() => ChangeOperationAxis());

            Cmd_TraceMovement = new DelegateCommand(() => TraceMovement());
            Cmd_ReferenceMovement = new DelegateCommand(() => ReferenceMovement());
            Cmd_OperationMovement = new DelegateCommand(() => OperationMovement());
        }

        private void ethernetClient_Received(object sender, EthernetClient.ReceivedEventArgs e)
        {
            if (app.StatusManager.CurrentNetworkStatus == StatusManager.NetworkStatus.Connected)
            {
                clientInfoQueue.Enqueue(e.EthernetClientInfo);
                if (clientInfoQueue.IsEmpty) return; // 데이터가 없다면 return

                EthernetClientInfo info = null;
                if (clientInfoQueue.TryDequeue(out info))
                {
                    try
                    {
                        Receiveinfo(info);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }
        void Receiveinfo(EthernetClientInfo info)
        {
            // Trace
            if (app.DeviceStatus)
            {
                if (app.CommonSetDataModel.Protocol == "Ascii") // Protocol 0:Hex, 1:Ascii
                {
                    int n = 0;
                    int len = info.Data.Length / 4; // data length
                    RecvFloatData = new float[len]; // Recv FData Init

                    for (int i = 0; i < info.Data.Length; i += 4)
                    {
                        RecvFloatData[n] = BitConverter.ToSingle(info.Data, i);
                        n++;
                    }

                    UpdateChartData(TRACE_Series, len); // Trace chart update.

                    AllowSendData = true;
                }
            }
            // Reference & Operation
            else
            {
                if (app.CommonSetDataModel.Protocol == "Ascii") // Protocol 0:Hex, 1:Ascii
                {
                    if (OperationCnt == app.ConfigureSetDataModel.SweepNum) // [DSTS] complete count
                    {
                        int start = app.ReferenceSetDataModel.Ref_Ds;
                        int end = app.ReferenceSetDataModel.Ref_Dd;
                        if (start > end)
                        {
                            MessageBox.Show("※ 감지영역 끝지점이 시작지점보다 작습니다.", "DSS System Message", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        int vn = 0;
                        int on = 0;
                        int len = end - start; // 측정 구간

                        // Reference 데이터
                        if (ReferenceButtonBusy)
                        {
                            for (int i = 0; i < info.Data.Length / 4; i++)
                            {
                                RecvFloatData[i] = BitConverter.ToSingle(info.Data, i * 4);

                                vn++;
                            }
                            if (SaveReference)
                            {
                                SaveRefDataToDb(info.Data); // 데이터 압축 후 저장
                            }
                            UpdateChartData(OPER_Series, info.Data.Length / 4);
                            app.ReferenceSetDataModel = app.DbManager.LoadReferenceData();

                            TraceButtonEnable = true;
                            ReferenceButtonBusy = false;
                            ReferenceButtonEnable = true;
                            OperationButtonEnable = true;
                        }
                        // Operation 데이터
                        else if (OperationButtonBusy || !AllowSendData)
                        {
                            byte[] refdata = app.DbManager.LoadReferenceValue(len); // 참조 데이터 불러오기
                            if (refdata.Length == info.Data.Length)
                            {
                                byte[] O_Value = info.Data; // operation 데이터
                                byte[] O_Diff = new byte[len * sizeof(float)];
                                RecvFloatData = new float[info.Data.Length / 4];
                                if (refdata.Length == info.Data.Length)
                                {
                                    for (int i = 0; i < info.Data.Length / 4; i++)
                                    {
                                        RecvFloatData[i] = BitConverter.ToSingle(info.Data, i * 4);

                                        if (i >= start && i < end)
                                        {
                                            RecvFloatData[i] = BitConverter.ToSingle(info.Data, i * 4) - BitConverter.ToSingle(refdata, i * 4); // difference

                                            Array.Copy(BitConverter.GetBytes(RecvFloatData[i]), 0, O_Diff, on * 4, sizeof(float)); // difference

                                            on++;
                                        }
                                    }

                                    if (SaveOperation) // 데이터 압축 후 저장
                                    {
                                        app.DbManager.UpdateOperationData(O_Value, O_Diff);
                                    }

                                    UpdateChartData(OPER_Series, app.DeviceRange);
                                    AllowSendData = true; // Recevied 대기
                                }
                                else
                                {
                                    return;
                                }
                            }
                            else
                            {

                                OperationButtonBusy = false;
                                AllowSendData = true;
                                MessageBox.Show("Reference Range값이 Operation 값이랑 다릅니다.\n Reference를 저장해주세요.", "DSS System Message", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        OperationCnt = 0;
                    }
                    else // [DSTS] reference or operation loading count
                    {
                        OperationCnt++;
                        OperationStr = $"DSTS Count : {app.ConfigureSetDataModel.SweepNum} to {OperationCnt} ..";
                    }
                }
            }
        }
        private void Send_DoWork(object sender, DoWorkEventArgs e)
        {
            if (app.DeviceStatus) // Trace
            {
                while (app.DeviceConnection && TraceButtonBusy) // DSTS연결 & Trace 
                {
                    if (AllowSendData) // Received 대기
                    {
                        if (TraceMode) // Pair mode
                        {
                            app.ConvertManager.SendData(app.ConvertManager.StringToByte($"MEAS:TRAC:DPP\r\n"), ethernetClient);
                        }
                        else // Single mode
                        {
                            app.ConvertManager.SendData(app.ConvertManager.StringToByte($"MEAS:TRAC:SPUL\r\n"), ethernetClient);
                        }
                        AllowSendData = false; // Recevied 종료
                    }
                }
            }
            else // Frequnecy
            {
                while (app.DeviceConnection && (OperationButtonBusy || OperationCnt != app.ConfigureSetDataModel.SweepNum)) // 연결 & (Operation || sweep num까지 대기)
                {
                    if (AllowSendData) // Recevied 대기
                    {
                        OperationCnt = 0; // Operation Count Initialize

                        app.ConvertManager.SendData(app.ConvertManager.StringToByte($"MEAS:FREQ:DPP\r\n"), ethernetClient);

                        AllowSendData = false; // Recevied 종료
                    }
                }
            }
        }
        private void RequestThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(1000);

                TraceButtonEnable = true;
                ReferenceButtonEnable = true;
                OperationButtonEnable = true;
                AllowOperationData = false;
            });
        }

        private void TraceMovement()
        {
            if (TraceButtonBusy) // 시작
            {
                if (app.DeviceConnection)
                {
                    ReferenceButtonEnable = false; // reference false
                    OperationButtonEnable = false; // operation false

                    app.DeviceStatus = true; // trace
                    AllowSendData = true; // allow data

                    app.DeviceRange = Convert.ToInt32(app.ConfigureSetDataModel.Range / app.ConfigureSetDataModel.SampInterval);
                    RecvFloatData = new float[app.DeviceRange];
                    CurrentBuffer = new double[WaterfallCnt, app.DeviceRange];
                    PastBuffer = new double[WaterfallCnt, app.DeviceRange];

                    TRACE_XMinEdit = 0; TRACE_XMaxEdit = app.DeviceRange;
                    TRACE_YMinEdit = 0; TRACE_YMaxEdit = 17000;
                    TRACE_XVisibleRange.SetMinMax(TRACE_XMinEdit, TRACE_XMaxEdit);
                    TRACE_YVisibleRange.SetMinMax(TRACE_YMinEdit, TRACE_YMaxEdit);
                    TRACE_UniformHeatmapDataSeries = new UniformHeatmapDataSeries<int, int, double>(CurrentBuffer, 0, 1, 0, 1); // Heatmap 초기화

                    RequestThread.RunWorkerAsync();

                    app.UpdatingVariableUpdatedSignal(1000);
                }
                else
                {
                    MessageBox.Show("장비와 연결을 확인해주세요.", "DSS System Message", MessageBoxButton.OK, MessageBoxImage.Information);

                    RequestThread.CancelAsync();
                    app.UpdatingVariableUpdatedSignal(1000);
                }
            }
            else
            {
                RequestThread.CancelAsync();

                app.UpdatingVariableUpdatedSignal(1000);
            }
        }
        private void ReferenceMovement()
        {
            if (ReferenceButtonBusy) // 시작
            {
                if (app.DeviceConnection) // 연결 & frequnecy 진행 X
                {
                    TraceButtonEnable = false;
                    OperationButtonEnable = false;

                    // 초기 설정
                    OperationCnt = 0;
                    app.DeviceStatus = false; // Measure Mode => Frequency
                    AllowOperationData = false;

                    app.DeviceRange = Convert.ToInt32(app.ConfigureSetDataModel.Range / app.ConfigureSetDataModel.SampInterval); // Range 설정
                    RecvFloatData = new float[app.DeviceRange];
                    CurrentBuffer = new double[WaterfallCnt, app.DeviceRange]; // current buffer Initialize
                    PastBuffer = new double[WaterfallCnt, app.DeviceRange]; // last buffer Initialize

                    OPER_XMinEdit = app.ReferenceSetDataModel.Ref_Ds; OPER_XMaxEdit = app.ReferenceSetDataModel.Ref_Dd;
                    OPER_YMinEdit = (int)app.ConfigureSetDataModel.InitFreq; OPER_YMaxEdit = (int)((app.ConfigureSetDataModel.SweepInterval * app.ConfigureSetDataModel.SweepNum) + app.ConfigureSetDataModel.InitFreq);
                    OPER_ColorMinEdit = 0; OPER_ColorMaxEdit = (int)((app.ConfigureSetDataModel.SweepInterval * app.ConfigureSetDataModel.SweepNum) + app.ConfigureSetDataModel.InitFreq);
                    OPER_XVisibleRange.SetMinMax(OPER_XMinEdit, OPER_XMaxEdit); // Set X
                    OPER_YVisibleRange.SetMinMax(OPER_YMinEdit, OPER_YMaxEdit); // Set Y
                    OPER_UniformHeatmapDataSeries = new UniformHeatmapDataSeries<int, int, double>(CurrentBuffer, 0, 1, 0, 1);

                    app.ConvertManager.SendData(app.ConvertManager.StringToByte($"MEAS:FREQ:DPP\r\n"), ethernetClient);

                    app.UpdatingVariableUpdatedSignal(1000);
                }
                else
                {
                    MessageBox.Show("장비와 연결을 확인해주세요.", "DSS System Message", MessageBoxButton.OK, MessageBoxImage.Information);

                    RequestThread.CancelAsync();
                    app.UpdatingVariableUpdatedSignal(1000);
                }
            }
        }
        private void OperationMovement()
        {
            try
            {
                if (OperationButtonBusy)
                {
                    if (app.DeviceConnection) // 연결
                    {
                        // 다른버튼 사용 X
                        TraceButtonEnable = false;
                        ReferenceButtonEnable = false;

                        // 초기 설정
                        OperationCnt = 0;
                        AllowSendData = true;
                        AllowOperationData = true;
                        app.DeviceStatus = false; // Measure Mode => Frequency

                        // Buffer Range 설정
                        int length = Convert.ToInt32(app.ConfigureSetDataModel.Range / app.ConfigureSetDataModel.SampInterval);
                        app.DeviceRange = length;
                        RecvFloatData = new float[length];
                        CurrentBuffer = new double[WaterfallCnt, length]; // current buffer Initialize
                        PastBuffer = new double[WaterfallCnt, length]; // last buffer Initialize
                                                                       // Chart Range 설정
                        OPER_XMinEdit = app.ReferenceSetDataModel.Ref_Ds; OPER_XMaxEdit = app.ReferenceSetDataModel.Ref_Dd;
                        OPER_YMinEdit = -50; OPER_YMaxEdit = 50;
                        OPER_ColorMinEdit = -50; OPER_ColorMaxEdit = 50;
                        OPER_XVisibleRange.SetMinMax(OPER_XMinEdit, OPER_XMaxEdit);
                        OPER_YVisibleRange.SetMinMax(OPER_YMinEdit, OPER_YMaxEdit);
                        OPER_UniformHeatmapDataSeries = new UniformHeatmapDataSeries<int, int, double>(CurrentBuffer, 0, 1, 0, 1);

                        if (RequestThread.IsBusy) { return; }
                        RequestThread.RunWorkerAsync();
                        app.UpdatingVariableUpdatedSignal(1000);
                    }
                    else
                    {
                        MessageBox.Show("장비와 연결을 확인해주세요.", "DSS System Message", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        RequestThread.CancelAsync();
                        app.UpdatingVariableUpdatedSignal(1000);
                    }
                }
                else
                {
                    RequestThread.CancelAsync();

                    app.UpdatingVariableUpdatedSignal(1000);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        private void ChangeAddress()
        {
            app.DbManager.UpdateCommonData(app.CommonSetDataModel, 0);
        }
        private void SleepMode()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(
                $"CONT:SOUR:EN 0\r\n" + // 광원전류
                $"CONT:EDFA:BIAS 1 0\r\n" + // 펄스 증폭기전류
                $"CONT:EDFA:BIAS 2 0\r\n" // 신호 증폭기전류
                );
            app.ControlSetDataModel.LightStatus = false;
            app.TraceSetDataModel.Edfa1 = 0;
            app.TraceSetDataModel.Edfa2 = 0;

            app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), ethernetClient);

            app.DbManager.UpdateControlData(app.ControlSetDataModel, 2);
            app.DbManager.UpdateTraceData(app.TraceSetDataModel, 1);

            app.UpdatingVariableUpdatedSignal(3000);
        }
        private void SaveControl()
        {
            // DB 업데이트
            app.DbManager.UpdateControlData(app.ControlSetDataModel, 0);

            // 장비 입력
            StringBuilder sb = new StringBuilder();
            sb.Append(
                    $"CONT:SOUR:EN {app.ControlSetDataModel.LightStatus}\r\n" + // 광원전류
                    $"CONT:PULS:EN {app.ControlSetDataModel.PulseStatus}\r\n" // 펄스전류
                    );

            //app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), ethernetClient);
            app.UpdatingVariableUpdatedSignal(1000);
        }
        private void SaveConfigure()
        {
            // DB 업데이트
            app.DbManager.UpdateConfigData(app.ConfigureSetDataModel, 0);

            // 장비 입력
            StringBuilder sb = new StringBuilder();
            sb.Append(
                    $"CONF:ACQ:SRES {app.ConfigureSetDataModel.SpatRes}\r\n" + // 공간 분해능
                    $"CONF:ACQ:SPAT {app.ConfigureSetDataModel.SampInterval}\r\n" + // 공간 샘플링
                    $"CONF:ACQ:RANG {app.ConfigureSetDataModel.Range}\r\n" + // 계측거리
                    $"CONF:ACQ:INTE {app.ConfigureSetDataModel.IntegNum}\r\n" + // 적분 횟수
                    $"CONF:SWEE:INIT {app.ConfigureSetDataModel.InitFreq}\r\n" + // 시작 탐사주파수
                    $"CONF:SWEE:DEV {app.ConfigureSetDataModel.SweepInterval}\r\n" + // 탐사 주파수 스윕 간격
                    $"CONF:SWEE:COUN {app.ConfigureSetDataModel.SweepNum}\r\n" // 탐사주파수 스윕 횟수
                    );
            app.DeviceRange = (int)app.ConfigureSetDataModel.Range;
            Array.Resize(ref RecvFloatData, app.DeviceRange);

            //app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), ethernetClient);
            app.UpdatingVariableUpdatedSignal(1000);
        }
        private void SaveTrace()
        {
            StringBuilder sb = new StringBuilder();

            // DB 업데이트
            app.DbManager.UpdateTraceData(app.TraceSetDataModel, 0);

            // 장비 입력
            sb.Append(
                $"CONT:EDFA:BIAS 1 {app.TraceSetDataModel.Edfa1}\r\n" + // EDFA 1
                $"CONT:EDFA:BIAS 2 {app.TraceSetDataModel.Edfa2}\r\n" + // EDFA 2
                $"CONT:VOA:BIAS 1 {app.TraceSetDataModel.ABias1}\r\n" + // 감쇄기 바이어스1 (Attenuator Bias 1)
                $"CONT:VOA:BIAS 2 {app.TraceSetDataModel.ABias2}\r\n" + // 감쇄기 바이어스2(Attenuator Bias 2)
                $"CONT:EOM:FREQ {app.TraceSetDataModel.ProbFreq}\r\n" + // 탐사주파수(Probe Freq)
                $"CONT:EOM:BIAS {app.TraceSetDataModel.EomBias}\r\n" + // 변조 바이어스(Eom Bias)
                $"CONT:EOM:EN {app.TraceSetDataModel.EomSwitch}\r\n" // 변조 전원(Eom Switch)
                );

            //app.ConvertManager.SendData(app.ConvertManager.StringToByte(sb), ethernetClient);
            app.UpdatingVariableUpdatedSignal(1000);
        }
        private void ChangeTraceAxis()
        {
            TRACE_XVisibleRange.SetMinMax(TRACE_XMinEdit, TRACE_XMaxEdit);
            TRACE_YVisibleRange.SetMinMax(TRACE_YMinEdit, TRACE_YMaxEdit);
            TRACE_UniformHeatmapDataSeries = new UniformHeatmapDataSeries<int, int, double>(CurrentBuffer, 0, 1, 0, 1);

            app.MainView.Chart1.InvalidateElement();
        }
        private void ChangeOperationAxis()
        {
            app.DbManager.UpdateSystemData(app.SystemSetDataModel);

            OPER_XVisibleRange.SetMinMax(OPER_XMinEdit, OPER_XMaxEdit);
            OPER_YVisibleRange.SetMinMax(OPER_YMinEdit, OPER_YMaxEdit);
            OPER_UniformHeatmapDataSeries = new UniformHeatmapDataSeries<int, int, double>(CurrentBuffer, 0, 1, 0, 1);

            app.MainView.Chart2.InvalidateElement();
        }
    }
}
