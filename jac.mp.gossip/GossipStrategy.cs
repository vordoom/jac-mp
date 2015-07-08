using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jac.mp.gossip
{
    public class GossipStrategy : IStrategy
    {
        private readonly IGossipTransport _transport;
        private readonly Dictionary<Uri, MemberInfo> _membersList = new Dictionary<Uri, MemberInfo>();
        private ulong _heartbeat;

        public IEnumerable<Node> Nodes
        {
            get { throw new NotImplementedException(); }
        }

        public event EventHandler<Node> NodeJoined;
        public event EventHandler<Node> NodeFailed;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GossipStrategy()
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="transport"></param>
        public GossipStrategy(Uri node, IGossipTransport transport) 
            : this(new Uri[] { node }, transport)
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="transport"></param>
        public GossipStrategy(Uri[] nodes, IGossipTransport transport)
        {
            if (transport == null)
                throw new ArgumentNullException("transport");

            if (nodes == null)
                throw new ArgumentNullException("nodes");

            _transport = transport;
            _heartbeat = 0;

            foreach (var n in nodes)
            {
                if (n == null)
                    throw new ArgumentException("Uri must not be null.");

                _membersList.Add(n, 
                    new MemberInfo()
                    {
                        Heartbeat = 0,
                        State = MemberState.Ok,
                        Timestamp = DateTime.Now.Ticks
                    });
            }
        }

        /// <summary>
        /// Update membership list.
        /// </summary>
        public void Update()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnNodeJoined()
        {
            NodeJoined(this, null);
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnNodeFailed()
        {
            NodeFailed(this, null);
        }
    }

    public class MemberInfo
    {
        public long Heartbeat { get; set; }
        public long Timestamp { get; set; }
        public MemberState State { get; set; }
    }

    public enum MemberState
    {
        Ok,
        Suspected,
        Failed
    }
}
