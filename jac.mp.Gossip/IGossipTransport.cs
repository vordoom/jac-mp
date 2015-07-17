using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jac.mp.Gossip
{
    public interface IGossipTransport
    {
        KeyValuePair<Uri, long>[] Ping(Uri nodeUri, KeyValuePair<Uri, long>[] keyValuePair);
    }
}
