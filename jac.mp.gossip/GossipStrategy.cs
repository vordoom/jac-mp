using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace jac.mp.gossip
{
    // todo: push, pull, push/pull mechanism
    // todo: prevent duplicate pings
    // todo: concurentDictionary
    // todo: check other multythreading issues

    public class GossipStrategy : IStrategy
    {
        private const int NumbersOfReceivers = 2;

        private readonly IGossipTransport _transport;
        private readonly Dictionary<Uri, MemberInfo> _membersList = new Dictionary<Uri, MemberInfo>();
        private readonly Random _random;
        private readonly Uri _ownUri;
        private long _heartbeat;

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
            _random = new Random();
            _ownUri = new Uri("127.0.0.1");

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
            // ping random nodes
            int number = NumbersOfReceivers < _membersList.Count ? NumbersOfReceivers : _membersList.Count;
            for (int i = 0; i < number; i++)
            {
                var index = _random.Next(number);
                var nodeUri = _membersList.Keys.ElementAt(index);

                Ping(nodeUri);
            }

            // process failed nodes
        }

        private void Ping(Uri nodeUri)
        {
            var dict = GetMembersDictionary();

            _transport.Ping(nodeUri, dict.ToArray());
        }

        private Dictionary<Uri,long> GetMembersDictionary()
        {
            Interlocked.Increment(ref _heartbeat);

            var dict = _membersList.ToDictionary(a => a.Key, a => a.Value.Heartbeat);
            dict.Add(_ownUri, _heartbeat);

            return dict;
        }



        /// <summary>
        /// 
        /// </summary>
        private void OnNodeJoined()
        {
            NodeJoined.Raise(this, null);
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnNodeFailed()
        {
            NodeFailed.Raise(this, null);
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
