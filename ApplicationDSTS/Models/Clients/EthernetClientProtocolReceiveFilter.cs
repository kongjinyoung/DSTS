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


        protected override int GetBodyLengthFromHeader(IBufferStream bufferStream, int length)
        {
            throw new NotImplementedException();
        }

        public override EthernetClientInfo ResolvePackage(IBufferStream bufferStream)
        {
            throw new NotImplementedException();
        }
    }
}
