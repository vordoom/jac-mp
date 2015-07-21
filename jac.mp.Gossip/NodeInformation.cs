using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jac.mp.Gossip
{
    [Serializable]
    public class NodeInformation
    {
        public Uri Address { get; set; }
        public long Hearbeat { get; set; }
    }
}
