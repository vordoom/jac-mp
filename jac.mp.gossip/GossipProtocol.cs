using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jac.mp.gossip
{
    public class GossipProtocol<TNodeData> : IMembershipProtocol<TNodeData>
    {
        public IEnumerable<Node<TNodeData>> Nodes
        {
            get { throw new NotImplementedException(); }
        }

        public event EventHandler<Node<TNodeData>> NodeJoined;

        public event EventHandler<Node<TNodeData>> NodeFailed;

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        private void OnNodeJoined()
        {
            this.NodeJoined(this, null);
        }

        private void OnNodeFailed()
        {
            this.NodeFailed(this, null);
        }
    }
}
