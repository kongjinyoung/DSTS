using ApplicationDSTS.Models.DataModels;
using SuperSocket.ProtoBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ApplicationDSTS.Models.Clients
{
    public class EthernetClientProtocolReceiveFilter : FixedHeaderReceiveFilter<EthernetClientInfo>
    {
        App app = Application.Current as App;
        private const int FixedHeaderSize = 2;

        public EthernetClientProtocolReceiveFilter()
            : base(FixedHeaderSize)
        {

        }



        public override EthernetClientInfo ResolvePackage(IBufferStream bufferStream)
        {
            try
            {
                App app = Application.Current as App;

                int lastBuffer = bufferStream.Buffers.Count - 1;
                int l = Convert.ToInt32(app.ConfigureSetDataModel.Range / app.ConfigureSetDataModel.SampInterval) * 4;
                byte[] data = bufferStream.Buffers[lastBuffer].Array;

                if (app.CommonSetDataModel.Protocol == "HEX") // True => Hex
                {
                    if (MainModel.OperationCnt == app.ConfigureSetDataModel.SweepNum || app.DeviceStatus) // Trace && All Count Frequency
                    {
                        byte[] buff = data.Take(l).ToArray();

                        return new EthernetClientInfo(buff);
                    }
                    else if (!app.DeviceStatus)// Frequency
                    {
                        byte[] buff = data.Take(l).ToArray();
                        for (int i = 0; i < bufferStream.Buffers[lastBuffer].Array.Length; i++)
                        {
                            if (bufferStream.Buffers[lastBuffer].Array[i] == 10)
                            {
                                l = i;
                                break;
                            }
                        }
                        buff = data.Take(l + 1).ToArray();

                        return new EthernetClientInfo(buff);
                    }
                }
                // Ascii
                else
                {
                    l = 0;
                    for (int i = 0; i < bufferStream.Buffers[lastBuffer].Array.Length; i++)
                    {
                        if (bufferStream.Buffers[lastBuffer].Array[i] == 10)
                        {
                            l = i;
                            break;
                        }
                    }
                    byte[] buff = data.Take(l + 1).ToArray();
                    return new EthernetClientInfo(buff);
                }
                return new EthernetClientInfo(data);
            }
            catch (Exception)
            {
                throw;
            }
        }
        protected override int GetBodyLengthFromHeader(IBufferStream bufferStream, int length)
        {
            try
            {
                int l = 0;
                int lastBuffer = bufferStream.Buffers.Count - 1;

                if (app.CommonSetDataModel.Protocol == "HEX" && app.DeviceConnection) // Hex & Device Connect
                {
                    if (app.DeviceStatus) // T:Trace
                    {
                        l = Convert.ToInt32(app.ConfigureSetDataModel.Range / app.ConfigureSetDataModel.SampInterval) * 4; // byte length

                        return l - 2;
                    }
                    else // F:Operation
                    {
                        for (int i = 0; i < bufferStream.Buffers[lastBuffer].Array.Length; i++)
                        {
                            if (MainModel.OperationCnt == app.ConfigureSetDataModel.SweepNum)
                            {
                                l = Convert.ToInt32(app.ConfigureSetDataModel.Range / app.ConfigureSetDataModel.SampInterval) * 4; // byte length

                                return l - 2;
                            }
                            // CR/LF확인
                            else if (bufferStream.Buffers[lastBuffer].Array[i] == 10 && bufferStream.Buffers[lastBuffer].Array[i - 1] == 13)
                            {
                                l = i;
                                break;
                            }
                        }
                        return l - 1;
                    }
                }
                else // Ascii
                {
                    for (int i = 0; i < bufferStream.Buffers[lastBuffer].Array.Length; i++)
                    {
                        // CR/LF확인
                        if (bufferStream.Buffers[lastBuffer].Array[i] == 10 && bufferStream.Buffers[lastBuffer].Array[i - 1] == 13)
                        {
                            l = i;
                            break;
                        }
                    }
                    return l - 1;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
