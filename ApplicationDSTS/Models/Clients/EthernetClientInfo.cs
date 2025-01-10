using SuperSocket.ProtoBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationDSTS.Models.Clients
{
    public class EthernetClientInfo : IPackageInfo
    {
        public byte[] Data { get; private set; }

        //public ushort End { get; private set; }

        public EthernetClientInfo(byte[] data)
        {
            this.Data = data;
        }
    }
}
