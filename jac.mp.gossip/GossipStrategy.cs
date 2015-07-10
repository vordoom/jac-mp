using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using log4net;

namespace jac.mp.gossip
{
    // todo: node fail
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
        private readonly Node _ownData;
        private readonly ILog _log = null;
        private long _heartbeat;
        private long _timeStamp;
        private IEnumerable<Node> _nodes;

        public IEnumerable<Node> Nodes
        {
            get { return _nodes; }
        }

        public event EventHandler<Node> NodeJoined;
        public event EventHandler<Node> NodeFailed;

        /// <summary>
        /// Constructor.
        /// </summary>
        public GossipStrategy() { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="transport"></param>
        public GossipStrategy(Node ownData, Uri node, IGossipTransport transport) 
            : this(ownData, new Uri[] { node }, transport)
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="transport"></param>
        public GossipStrategy(Node ownData, Uri[] nodes, IGossipTransport transport)
        {
            if (transport == null)
                throw new ArgumentNullException("transport");

            if (nodes == null)
                throw new ArgumentNullException("nodes");

            _transport = transport;
            _heartbeat = 0;
            _timeStamp = 0;
            _random = new Random();
            _ownData = ownData;
            _log = LogManager.GetLogger(this.GetType());
            _nodes = _membersList.Values.Select(a => a.NodeData);

            foreach (var n in nodes)
            {
                if (n == null)
                    throw new ArgumentException("Uri must not be null.");

                AddNewNode(n, 0);
            }
        }

        /// <summary>
        /// Update membership list.
        /// </summary>
        public void Update()
        {
            // todo: concurrency
            _timeStamp++;

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeUri"></param>
        private void Ping(Uri nodeUri)
        {
            Interlocked.Increment(ref _heartbeat);

            var dict = GetMembersDictionary();

            try
            {
                var result = _transport.Ping(nodeUri, dict.ToArray());

                UpdateMembers(result);
            }
            catch (Exception ex)
            {
                _log.Debug(String.Format("Failed to ping node {0}", nodeUri), ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        private void UpdateMembers(KeyValuePair<Uri, long>[] result)
        {
            foreach (var kv in result)
            {
                if (_membersList.ContainsKey(kv.Key))
                {
                    _membersList[kv.Key].Heartbeat = kv.Value;
                    _membersList[kv.Key].Timestamp = _timeStamp;
                }
                else
                {
                    AddNewNode(kv.Key, kv.Value);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="heartbeat"></param>
        private void AddNewNode(Uri uri, long heartbeat)
        {
            var node = new MemberInfo()
            {
                Heartbeat = heartbeat,
                State = MemberState.Ok,
                Timestamp = _timeStamp,
                NodeData = new Node(uri)
            };

            _membersList.Add(uri, node);
            
            OnNodeJoined(node.NodeData);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Dictionary<Uri,long> GetMembersDictionary()
        {
            var dict = _membersList.ToDictionary(a => a.Key, a => a.Value.Heartbeat);
            dict.Add(_ownData.Address, _heartbeat);

            return dict;
        }


        /// <summary>
        /// 
        /// </summary>
        private void OnNodeJoined(Node node)
        {
            NodeJoined.Raise(this, node);
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnNodeFailed(Node node)
        {
            NodeFailed.Raise(this, node);
        }
    }

    public class MemberInfo
    {
        public long Heartbeat { get; set; }
        public long Timestamp { get; set; }
        public MemberState State { get; set; }
        public Node NodeData { get; set; }
    }

    public enum MemberState
    {
        Ok,
        Suspected,
        Failed
    }
}
