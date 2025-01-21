using ApplicationDSTS.Models.DataModels;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ApplicationDSTS.Models.Managers
{
    public class DbManager
    {
        #region Database Connect

        private string strConn = @"
                        HOST=localhost;
                        PORT=5432;
                        USERNAME=postgres;
                        PASSWORD=12345678;
                        DATABASE=DSS_ANALYSISDB;";
        public void ExecuteNonQuery(string sql)
        {
            try
            {
                using (var conn = new NpgsqlConnection(strConn))
                {
                    conn.Open();
                    using (var command = new NpgsqlCommand(sql, conn))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("==============================================================================================");
                System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString());
                System.Diagnostics.Debug.WriteLine("Message : " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Trace   : " + ex.StackTrace + ex.StackTrace.Substring(ex.StackTrace.LastIndexOf(' ')));
                System.Diagnostics.Debug.WriteLine("==============================================================================================");
            }
        }
        public DataSet GetDataSetByQuery(string sql)
        {
            DataSet ds = new DataSet();
            try
            {
                using (var conn = new NpgsqlConnection(strConn))
                {
                    using (var adpt = new NpgsqlDataAdapter(sql, conn))
                    {
                        adpt.Fill(ds);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("==============================================================================================");
                System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString());
                System.Diagnostics.Debug.WriteLine("Message : " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Trace   : " + ex.StackTrace + ex.StackTrace.Substring(ex.StackTrace.LastIndexOf(' ')));
                System.Diagnostics.Debug.WriteLine("==============================================================================================");
            }
            return ds;
        }

        #endregion


        #region ■■■■■■■■■ System Set ■■■■■■■■■
        public SystemSetDataModel LoadSystemData()
        {
            SystemSetDataModel data = new SystemSetDataModel();
            string sql = $"select * from tb_systemset";
            try
            {
                DataSet ds = GetDataSetByQuery(sql);

                data.HThreshold = Convert.ToInt32(ds.Tables[0].Rows[0][1].ToString());
                data.LThreshold = Convert.ToInt32(ds.Tables[0].Rows[0][2].ToString());
            }
            catch (Exception)
            {
                data.HThreshold = 10;
                data.LThreshold = -10;
            }

            return data;
        }
        public void UpdateSystemData(SystemSetDataModel system)
        {
            SystemSetDataModel data = new SystemSetDataModel();
            string sql = $"update tb_systemset set hthreshold={system.HThreshold}, lthreshold={system.LThreshold}, last_update=now() where sysid = 1";
            ExecuteNonQuery(sql);
        }
        #endregion

        #region ■■■■■■■■■ Common Set ■■■■■■■■■

        public CommonSetDataModel LoadCommonData()
        {
            CommonSetDataModel data = new CommonSetDataModel();
            string sql = $"select * from tb_commonset";
            DataSet ds = GetDataSetByQuery(sql);

            try
            {
                // DAS Address Information
                data.IpAddress = ds.Tables[0].Rows[0][1].ToString();
                data.Port = Convert.ToInt32(ds.Tables[0].Rows[0][2].ToString());
                data.Protocol = ds.Tables[0].Rows[0][3].ToString();
                data.Path = ds.Tables[0].Rows[0][4].ToString();
            }
            catch (Exception)
            {
                data.IpAddress = "192.168.1.46";
                data.Port = 5007;
                data.Protocol = "Hex";
            }

            return data;
        }

        public void UpdateCommonData(CommonSetDataModel comm, int set)
        {
            string sql = string.Empty;
            switch (set)
            {
                case 0: // save ip,port,protocol mode
                    sql = $"update tb_commonset set ip='{comm.IpAddress}', port={comm.Port}, mode='{comm.Protocol}', last_update=now() where commid  = 1";
                    break;
                case 1: // save path
                    sql = $"update tb_commonset set path='{comm.Path}', last_update=now() where commid = 1";
                    break;
            }
            ExecuteNonQuery(sql);
        }

        #endregion

        /// <summary>
        /// DSTS Data Models
        /// </summary>
        /// <returns></returns>
        #region ■■■■■■■■■ DSTS Control Set ■■■■■■■■■

        public ControlSetDataModel LoadControlData()
        {
            ControlSetDataModel data = new ControlSetDataModel();
            string sql = @"select * from tb_controlset where ctrlid=1";
            DataSet ds = GetDataSetByQuery(sql);
            try
            {
                data.LightStatus = Convert.ToBoolean(ds.Tables[0].Rows[0][1].ToString());
                data.PulseStatus = Convert.ToBoolean(ds.Tables[0].Rows[0][2].ToString());
            }
            catch (Exception)
            {
                if(ds == null)
                {
                    data.LightStatus = false;
                    data.PulseStatus = false;
                }
            }

            return data;
        }
        public void UpdateControlData(ControlSetDataModel control, int set)
        {
            string sql = string.Empty;
            switch (set)
            {
                case 0: // save ldstatus
                    sql = $"update tb_controlset set " +
                        $"ldstatus={control.LightStatus},last_update=now() where ctrlid = 1";
                    break;
                case 1: // save pulsestatus
                    sql = $"update tb_controlset set " +
                        $"pulsstatus={control.PulseStatus},last_update=now() where ctrlid = 1";
                    break;
                case 2: // sleep mode (ld - off)
                    sql = $"update tb_controlset set ldstatus=false last_update=now() where ctrlid = 1";
                    break;
            }
            ExecuteNonQuery(sql);
        }

        #endregion

        #region ■■■■■■■■■ DSTS Configure Set ■■■■■■■■■

        public ConfigureSetDataModel LoadConfigureData()
        {
            ConfigureSetDataModel data = new ConfigureSetDataModel();
            string sql = @"select * from tb_configset where confid=1";
            try
            {
                DataSet ds = GetDataSetByQuery(sql);

                data.SpatRes = Convert.ToSingle(ds.Tables[0].Rows[0][1].ToString()); // 공간 분해능
                data.SampInterval = Convert.ToSingle(ds.Tables[0].Rows[0][2].ToString()); // 공간 샘플링
                data.Range = Convert.ToSingle(ds.Tables[0].Rows[0][3].ToString()); // 계측거리
                data.IntegNum = Convert.ToInt32(ds.Tables[0].Rows[0][4].ToString()); // 적분 횟수
                data.InitFreq = Convert.ToSingle(ds.Tables[0].Rows[0][5].ToString()); // 시작 주파수
                data.SweepInterval = Convert.ToSingle(ds.Tables[0].Rows[0][6].ToString()); // 탐사 주파수 스윕 간격
                data.SweepNum = Convert.ToSingle(ds.Tables[0].Rows[0][7].ToString()); // 탐사 주파수 스윗 횟수
            }
            catch (Exception)
            {
                data.SpatRes = 1;
                data.SampInterval = 0.5f;
                data.Range = 1000;
                data.IntegNum = 2526;
                data.InitFreq = 10650;
                data.SweepInterval = 5;
                data.SweepNum = 61;
            }
            return data;
        }
        public void UpdateConfigData(ConfigureSetDataModel config, int set)
        {
            string sql = string.Empty;
            switch (set)
            {
                case 0: // save spatres, sampinterval, range, integnum, initfreq, swpinterval, swpnum
                    sql = $"update tb_configset set spatres={config.SpatRes},sampinterval={config.SampInterval},range={config.Range},integnum={config.IntegNum}," +
                        $"initfreq={config.InitFreq},swpinterval={config.SweepInterval},swpnum={config.SweepNum},last_update=now() where confid = 1";
                    break;
            }
            ExecuteNonQuery(sql);
        }

        #endregion

        #region ■■■■■■■■■ Trace Set ■■■■■■■■■

        public TraceSetDataModel LoadTraceData()
        {
            TraceSetDataModel data = new TraceSetDataModel();
            string sql = @"select * from tb_traceset where traceid=1";
            try
            {
                DataSet ds = GetDataSetByQuery(sql);

                data.Edfa1 = Convert.ToSingle(ds.Tables[0].Rows[0][1].ToString());
                data.Edfa2 = Convert.ToSingle(ds.Tables[0].Rows[0][2].ToString());
                data.ABias1 = Convert.ToSingle(ds.Tables[0].Rows[0][3].ToString());
                data.ABias2 = Convert.ToSingle(ds.Tables[0].Rows[0][4].ToString());
                data.ProbFreq = Convert.ToSingle(ds.Tables[0].Rows[0][5].ToString());
                data.EomBias = Convert.ToSingle(ds.Tables[0].Rows[0][6].ToString());
                data.EomSwitch = Convert.ToBoolean(ds.Tables[0].Rows[0][7].ToString());
            }
            catch (Exception)
            {
                data.Edfa1 = 0;
                data.Edfa2 = 0;
                data.ABias1 = 1000;
                data.ABias2 = 1000;
                data.ProbFreq = 10800;
                data.EomBias = 2048;
                data.EomSwitch = false; // 0
            }
            return data;
        }
        public void UpdateTraceData(TraceSetDataModel trace, int set)
        {
            string sql = string.Empty;
            switch (set)
            {
                case 0:
                    sql = $"update tb_traceset set edfa1={trace.Edfa1},edfa2={trace.Edfa2},abias1={trace.ABias1},abias2={trace.ABias2}," +
                        $"probfreq={trace.ProbFreq},eombias={trace.EomBias},eomswitch={trace.EomSwitch},last_update=now() where traceid = 1";
                    break;
                case 1: // Sleep Mode
                    sql = $"update tb_traceset set edfa1={trace.Edfa1},edfa2={trace.Edfa2},last_update=now() where traceid = 1";
                    break;
            }
            ExecuteNonQuery(sql);
        }

        #endregion

        #region ■■■■■■■■■ Reference Set ■■■■■■■■■

        public ReferenceSetDataModel LoadReferenceData()
        {
            ReferenceSetDataModel data = new ReferenceSetDataModel();
            try
            {
                string sql = @"select refid, ts, td, temp, ds, dd, last_update from tb_referenceset order by refid desc";
                DataSet ds = GetDataSetByQuery(sql);

                data.Ref_Ts = Convert.ToInt32(ds.Tables[0].Rows[0][1].ToString());
                data.Ref_Td = Convert.ToInt32(ds.Tables[0].Rows[0][2].ToString());
                data.Ref_Temp = Convert.ToSingle(ds.Tables[0].Rows[0][3].ToString());
                data.Ref_Ds = Convert.ToInt32(ds.Tables[0].Rows[0][4].ToString());
                data.Ref_Dd = Convert.ToInt32(ds.Tables[0].Rows[0][5].ToString());
                data.LastUpdate = ds.Tables[0].Rows[0][6].ToString().Substring(0, 21); // YYYY-MM-dd HH:mm:ss
            }
            catch (Exception)
            {
                data.Ref_Ts = 0;
                data.Ref_Td = 0;
                data.Ref_Temp = 0;
                data.Ref_Ds = 0;
                data.Ref_Dd = 0;
                data.LastUpdate = "";
            }
            return data;
        }
        public void InsertReferenceData(ReferenceSetDataModel reference)
        {
            string sql = string.Empty;
            int id;
            try
            {
                sql = "select refid from tb_referenceset order by refid desc";
                id = int.Parse(GetDataSetByQuery(sql).Tables[0].Rows[0][0].ToString()) + 1; // 데이터가 O
            }
            catch (Exception)
            {
                id = 1; // 데이터 x
            }

            sql = $"insert into tb_referenceset values('{id}', '{reference.Ref_Ts}', '{reference.Ref_Td}', '{reference.Ref_Temp}', '{reference.Ref_Ds}', '{reference.Ref_Dd}', '{reference.Ref_Value}', now())";

            ExecuteNonQuery(sql);
        }

        public void DeleteReferenceData() // 테이블 초기화
        {
            string sql = "truncate table tb_referenceset";

            ExecuteNonQuery(sql);
        }
        #endregion

        #region ■■■■■■■■■ Operation Set ■■■■■■■■■

        public byte[] LoadReferenceValue(int length)
        {
            App app = Application.Current as App;
            byte[] DecompressData = new byte[length * sizeof(float)];
            try
            {
                string sql = $"select val from tb_referenceset order by refid desc";
                DataSet ds = GetDataSetByQuery(sql);

                string data = ds.Tables[0].Rows[0][0].ToString();

                DecompressData = app.DbManager.Decompress(app.ConvertManager.HexToByte(data));

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "DSS System Message", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return DecompressData;
        }

        public void UpdateOperationData(byte[] value, byte[] difference)
        {
            App app = Application.Current as App;
            string sql = string.Empty;
            int id;

            byte[] compressedValue = Compress(value); // value
            byte[] compressedDiff = Compress(difference); // difference
            try
            {
                sql = "select oper_id from tb_operation order by oper_id desc";
                id = int.Parse(GetDataSetByQuery(sql).Tables[0].Rows[0][0].ToString()) + 1;
            }
            catch (Exception)
            {
                id = 1;
            }
            sql = $"insert into tb_operation values(" +
                  $"{id}, " +
                  $"{app.ReferenceSetDataModel.Ref_Ds}, " +
                  $"{app.ReferenceSetDataModel.Ref_Dd}, " +
                  $"'{app.ConvertManager.ByteToHexString(compressedValue)}', " +
                  $"'{app.ConvertManager.ByteToHexString(compressedDiff)}', " +
                  $"now())";

            ExecuteNonQuery(sql);
        }

        #endregion

        #region ■■■■■■■■■ 데이터 검색 ■■■■■■■■■

        public void LoadHistoryEventData(string startTime, string endTime, int s_loc, int e_loc)
        {
            App app = Application.Current as App;

            string sql = string.Empty;
            if (s_loc > 0 && e_loc > 0)
            {
                sql = $"select * from tb_operation where last_update >= '{startTime}' and last_update < '{endTime}' and ref_ds = {s_loc} and ref_dd = {e_loc}";
            }
            else if (s_loc > 0)
            {
                sql = $"select * from tb_operation where last_update >= '{startTime}' and last_update < '{endTime}' and ref_ds = {s_loc}";
            }
            else if (e_loc > 0)
            {
                sql = $"select * from tb_operation where last_update >= '{startTime}' and last_update < '{endTime}' and ref_dd = {e_loc}";
            }
            else
            {
                sql = $"select * from tb_operation where last_update >= '{startTime}' and last_update < '{endTime}'";
            }

            DataSet ds = GetDataSetByQuery(sql);

            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                HistoryEventData data = new HistoryEventData
                {
                    Index = i,
                    No = int.Parse(ds.Tables[0].Rows[i][0].ToString()),
                    Time = DateTime.Parse(ds.Tables[0].Rows[i][5].ToString()).ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    StartLocation = int.Parse(ds.Tables[0].Rows[i][1].ToString()),
                    EndLocation = int.Parse(ds.Tables[0].Rows[i][2].ToString()),
                };


                HistoryChartData chart = new HistoryChartData
                {
                    Value = Decompress(app.ConvertManager.HexToByte(ds.Tables[0].Rows[i][3].ToString())),
                    Difference = Decompress(app.ConvertManager.HexToByte(ds.Tables[0].Rows[i][4].ToString())),
                };

                app.HistoryEventData.Add(data);
                app.HistoryChartData.Add(chart);
            }
        }


        #endregion

        #region ■■■■■■■■■ 데이터 압축 ■■■■■■■■■
        public Byte[] Compress(Byte[] buffer)    // 데이터 압축
        {
            Byte[] compressedByte;
            using (MemoryStream ms = new MemoryStream())
            {
                using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress))
                {
                    ds.Write(buffer, 0, buffer.Length);
                }

                compressedByte = ms.ToArray();
            }
            return compressedByte;
        }

        public Byte[] Decompress(Byte[] buffer)  // 데이터 압축 풀기
        {
            MemoryStream resultStream = new MemoryStream();

            using (MemoryStream ms = new MemoryStream(buffer))
            {
                using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
                {
                    ds.CopyTo(resultStream);
                    ds.Close();
                }
            }
            Byte[] decompressedByte = resultStream.ToArray();
            resultStream.Dispose();

            return decompressedByte;
        }

        #endregion
    }
}
