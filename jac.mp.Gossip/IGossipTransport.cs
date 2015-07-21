using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jac.mp.Gossip
{
    public delegate KeyValuePair<Uri, long>[] PingRequestHandler(KeyValuePair<Uri, long>[] nodesInformation);

    public interface IGossipTransport
    {
        Uri LocalUri { get; }
        PingRequestHandler IncomingPingCallback { get; set; }

        KeyValuePair<Uri, long>[] Ping(Uri nodeUri, KeyValuePair<Uri, long>[] nodesInformation);
    }
}
