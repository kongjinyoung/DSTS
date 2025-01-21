using ApplicationDSTS.Models.Clients;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.ViewportManagers;
using SciChart.Data.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ApplicationDSTS.Models.DataModels
{
    public class MainModel : INotifyPropertyChanged
    {
        public App app = Application.Current as App;
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public BackgroundWorker RequestThread;
        public static EthernetClient ethernetClient { get; set; }
        public ConcurrentQueue<EthernetClientInfo> clientInfoQueue;

        // DSTS - 프로토콜 모드(0:Hex 1:Ascii)
        public ObservableCollection<string> ProtocolModeList { get; set; } = new ObservableCollection<string>()
        {
            "HEX", "ASCII"
        };
        public ObservableCollection<int> IntegNumList { get; set; } = new ObservableCollection<int>()
        {
            1,2,4,8,16,32,64,128,256,512,1024
        };
        public bool AllowSendData { get; set; } // T:데이터 전송 허용, F:데이터 전송 중지

        /// <summary>
        /// related to trace button.
        /// </summary>
        public bool TraceButtonBusy { get; set; } // T:시작, F:정지
        public bool TraceButtonEnable { get; set; } // T:사용가능, F:사용 불가능

        /// <summary>
        /// related to trace settings.
        /// </summary>
        public bool TraceMode { get; set; } // T:Pair Mode, F:Single Mode

        /// <summary>
        /// related to operation button.
        /// </summary>
        public bool ReferenceButtonBusy { get; set; } // T:시작, F:정지
        public bool ReferenceButtonEnable { get; set; } // T:사용가능, F:사용 불가능

        public bool OperationButtonBusy { get; set; } // T:시작, F:정지
        public bool AllowOperationData { get; set; } // T:오퍼레이션 사용허락, F:오퍼레이션 사용불가
        public bool OperationButtonEnable { get; set; } // T:사용가능, F:사용 불가능
        public static int OperationCnt { get; set; } // Operation, Reference Return Count
        public string OperationStr { get; set; } // Operation, Reference String

        /// <summary>
        /// related to operation settings.
        /// </summary>
        public bool SaveReference { get; set; } // T:save, F:don't save
        public bool SaveOperation { get; set; } // T:save, F:don't save


        #region # [Define] Common Chart Variable

        public int WaterfallCnt = 200;
        public double[] _im;
        public double[] _re;
        public float[] RecvFloatData;
        public bool SelectTrace { get; set; } // Select Trace Chart 
        public bool SelectOperation { get; set; } // Select Operation Chart
        public double[,] CurrentBuffer { get; set; }
        public double[,] PastBuffer { get; set; }

        #endregion

        #region # [Define] Trace Chart

        // related to trace chart.        
        public IViewportManager TRACE_ViewportManager { get; } = new DefaultViewportManager();
        public IXyDataSeries<double, double> TRACE_Series { get; set; }
        public IRange TRACE_XVisibleRange { get; set; }
        public IRange TRACE_YVisibleRange { get; set; }
        public IDataSeries TRACE_UniformHeatmapDataSeries { get; set; }

        public int TRACE_XMinEdit { get; set; }
        public int TRACE_XMaxEdit { get; set; }
        public int TRACE_YMinEdit { get; set; }
        public int TRACE_YMaxEdit { get; set; }
        public int TRACE_ColorMinEdit { get; set; }
        public int TRACE_ColorMaxEdit { get; set; }

        #endregion

        #region # [Define] Operation Chart

        // related to operation chart.
        public IViewportManager OPER_ViewportManager { get; } = new DefaultViewportManager();
        public IXyDataSeries<double, double> OPER_Series { get; set; }
        public IRange OPER_XVisibleRange { get; set; }
        public IRange OPER_YVisibleRange { get; set; }
        public IDataSeries OPER_UniformHeatmapDataSeries { get; set; }

        public int OPER_XMinEdit { get; set; }
        public int OPER_XMaxEdit { get; set; }
        public int OPER_YMinEdit { get; set; }
        public int OPER_YMaxEdit { get; set; }
        public int OPER_ColorMinEdit { get; set; }
        public int OPER_ColorMaxEdit { get; set; }

        #endregion

        #region # [Define] Command

        public ICommand Cmd_ChangeAddress { get; set; } // IP, Port Change
        public ICommand Cmd_SleepMode { get; set; } // DSTS Sleep Mode
        public ICommand Cmd_SaveControl { get; set; } // Save Control Settings
        public ICommand Cmd_SaveConfigure { get; set; } // Save Configure Settings
        public ICommand Cmd_SaveTrace { get; set; } // Save Trace Settings
        public ICommand Cmd_ChangeTraceAxis { get; set; } // Save Trace Settings
        public ICommand Cmd_ChangeOperationAxis { get; set; } // Save Trace Settings
        public ICommand Cmd_TraceMovement { get; set; } // Trace Movement
        public ICommand Cmd_ReferenceMovement { get; set; } // Reference Movement
        public ICommand Cmd_OperationMovement { get; set; } // Operation Movement

        #endregion

        #region # [Function] Related To MainView
        public void SaveRefDataToDb(byte[] Value)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    ReferenceSetDataModel rdm = new ReferenceSetDataModel();
                    byte[] compressedValue = app.DbManager.Compress(Value); // value

                    rdm.Ref_Ts = app.ReferenceSetDataModel.Ref_Ts;
                    rdm.Ref_Td = app.ReferenceSetDataModel.Ref_Td;
                    rdm.Ref_Temp = app.ReferenceSetDataModel.Ref_Temp;
                    rdm.Ref_Ds = app.ReferenceSetDataModel.Ref_Ds;
                    rdm.Ref_Dd = app.ReferenceSetDataModel.Ref_Dd;
                    rdm.Ref_Value = app.ConvertManager.ByteToHexString(compressedValue); // YValues To Ref Value

                    app.DbManager.InsertReferenceData(rdm); // 환경정보 저장
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "DSS System Message", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        // Chart
        public void FirstWaterfallSeries(IXyDataSeries<double, double> series, int range, double[,] pastBuffer)
        {
            for (int x = 0; x < WaterfallCnt; x++)
            {
                FirstUpdateXyDataSeries(series);
                for (int y = 0; y < range; y++)
                {
                    pastBuffer[x, y] = series.YValues[y];
                }
            }
        }
        private void FirstUpdateXyDataSeries(IXyDataSeries<double, double> series)  // Chart 초기 생성
        {
            var lineData = new XyDataSeries<double, double>();

            for (int i = 0; i < 100; i++)
            {
                lineData.Append(i, 0.0);
            }
            series = lineData;
        }
        public void UpdateChartData(IXyDataSeries<double, double> series, int length)
        {
            using (series.SuspendUpdates())
            {
                UpdateXyDataSeries(series, length);
            }
        }
        private void UpdateXyDataSeries(IXyDataSeries<double, double> series, int length)
        {
            _im = new double[length];
            _re = new double[length];

            if (series == TRACE_Series)
            {
                for (int i = 0; i < length; i++)
                {
                    _im[i] = i;
                    _re[i] = RecvFloatData[i];
                }
                series.Clear();
                series.Append(_im, _re);

                UpdateSpectrogramHeatmapSeries(series, length, CurrentBuffer, PastBuffer, TRACE_UniformHeatmapDataSeries);
            }
            else if (series == OPER_Series)
            {
                for (int i = 0; i < length; i++)
                {
                    _im[i] = i;
                    _re[i] = RecvFloatData[i];
                }
                series.Clear();
                series.Append(_im, _re);

                UpdateSpectrogramHeatmapSeries(series, length, CurrentBuffer, PastBuffer, OPER_UniformHeatmapDataSeries);
            }
        }
        private void UpdateSpectrogramHeatmapSeries(IXyDataSeries<double, double> series, int range, double[,] currBuffer, double[,] pastBuffer, IDataSeries uniformHeatmapDataSeries)
        {
            for (int x = 0; x < WaterfallCnt; x++)
            {
                for (int y = 0; y < range; y++)
                {
                    currBuffer[x, y] = (x == 0) ? series.YValues[y] : pastBuffer[x - 1, y];
                }
            }
            Array.Copy(currBuffer, pastBuffer, currBuffer.Length);
            uniformHeatmapDataSeries.InvalidateParentSurface(RangeMode.None);
        }

        #endregion
    }
}
