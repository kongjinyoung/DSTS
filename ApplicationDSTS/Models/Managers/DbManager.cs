using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                        DATABASE=DSS_ANALYSIS_DB;";
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
    }
}
