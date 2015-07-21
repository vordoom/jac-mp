using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jac.mp.Gossip
{
    public delegate NodeInformation[] PingRequestHandler(NodeInformation senderInformation, NodeInformation[] membersInformation);

    public interface IGossipTransport
    {
        Uri LocalUri { get; }
        PingRequestHandler IncomingPingCallback { get; set; }

        NodeInformation[] Ping(Uri targetUri, NodeInformation localInformation, NodeInformation[] membersInformation);
    }
}
