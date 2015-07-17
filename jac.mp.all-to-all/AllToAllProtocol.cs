using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jac.mp.AllToAll
{
    public class AllToAllProtocol : IMembershipProtocol
    {
        public IEnumerable<Node> Nodes
        {
            get { throw new NotImplementedException(); }
        }

        public event EventHandler<Node> NodeJoined;

        public event EventHandler<Node> NodeFailed;

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
