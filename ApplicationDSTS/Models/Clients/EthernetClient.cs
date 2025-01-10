using SuperSocket.ClientEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationDSTS.Models.Clients
{
    public class EthernetClient : EasyClient
    {
        public delegate void ReceivedEventHandler(object sender, ReceivedEventArgs e);
        public event ReceivedEventHandler Received = delegate { };
        public int ReceiveLength { get; set; }
        public EthernetClient()
        {
            this.Initialize();
        }
        public class ReceivedEventArgs
        {
            public EthernetClientInfo EthernetClientInfo { get; set; }
            public ReceivedEventArgs(EthernetClientInfo ethernetClientInfo)
            {
                this.EthernetClientInfo = ethernetClientInfo;
            }
        }
        private void ReceiveHandler(EthernetClientInfo info)
        {
            this.Received(this, new ReceivedEventArgs(info));
        }
        private void Initialize()
        {
            this.Initialize(new EthernetClientProtocolReceiveFilter(), (info) => ReceiveHandler(info));
        }
    }
}
