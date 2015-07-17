using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using log4net;
using System.Configuration;
using jac.mp.Gossip.Configuration;

namespace jac.mp.Gossip
{
    // todo: onfiguration
    // todo: push, pull, push/pull mechanism
    // todo: prevent duplicate pings
    // todo: concurentDictionary
    // todo: check other multythreading issues (client read Nodes / gossip updates members -> exception)
    // todo: should not be 'blackout' situation, when all nodes removed (no-one to ping)

    public class GossipStrategy : IStrategy
    {
        #region Static members.

        static GossipConfigurationSection GetGossipConfiguration()
        {
            return ConfigurationManager.GetSection(ConfigurationSectionName) as GossipConfigurationSection;
        } 

        #endregion

        private const string ConfigurationSectionName = "GossipConfiguration";

        private readonly int _numbersOfReceivers;
        private readonly int _failTimeout;
        private readonly int _removeTimeout;
        private readonly IGossipTransport _transport;
        private readonly Dictionary<Uri, MemberInfo> _membersList = new Dictionary<Uri, MemberInfo>();
        private readonly Random _random;
        private readonly Node _ownData;
        private readonly ILog _log = null;
        private long _heartbeat;
        private long _timeStamp;
        private IEnumerable<Node> _activeNodes;

        public event EventHandler<Node> NodeJoined;
        public event EventHandler<Node> NodeFailed;

        public IEnumerable<Node> Nodes
        {
            get { return _activeNodes; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GossipStrategy(Node ownData, Uri[] nodes, IGossipTransport transport)
            : this(ownData, nodes, transport, GetGossipConfiguration())
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="transport"></param>
        public GossipStrategy(Node ownData, Uri[] nodes, IGossipTransport transport, GossipConfigurationSection configuration)
        {
            if (transport == null)
                throw new ArgumentNullException("transport");

            if (nodes == null)
                throw new ArgumentNullException("nodes");

            if (configuration == null)
                throw new ArgumentNullException("configuration");

            _transport = transport;
            _numbersOfReceivers = configuration.PingsPerIteration;
            _failTimeout = configuration.FailTimeout;
            _removeTimeout = configuration.RemoveTimeout;
            _heartbeat = 0;
            _timeStamp = 0;
            _random = configuration.RandomSeed < 0 ? new Random() : new Random(configuration.RandomSeed);
            _ownData = ownData;
            _log = LogManager.GetLogger(this.GetType());
            _activeNodes = _membersList.Values.Select(a => a.NodeData);

            // setup intial nodes list
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
            int number = _numbersOfReceivers < _membersList.Count ? _numbersOfReceivers : _membersList.Count;
            for (int i = 0; i < number; i++)
            {
                var index = _random.Next(number);
                var nodeUri = _membersList.Keys.ElementAt(index);

                Ping(nodeUri);
            }

            // process not responding nodes -> mark as failed
            var result = _membersList.Where(a => a.Value.Timestamp < _timeStamp - _failTimeout).Select(a => a.Value);
            foreach (var v in result)
            {
                v.State = MemberState.Failed;
            }

            // process nodes to remove
            result = _membersList.Where(a => a.Value.Timestamp < _timeStamp - _removeTimeout).Select(a => a.Value).ToArray();
            foreach (var v in result)
            {
                var node = v.NodeData;
                _membersList.Remove(node.Address);

                OnNodeFailed(node);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeUri"></param>
        private void Ping(Uri nodeUri)
        {
            Interlocked.Increment(ref _heartbeat);

            var dict = _membersList.Where(a => a.Value.State == MemberState.Ok).ToDictionary(a => a.Key, a => a.Value.Heartbeat);
            dict.Add(_ownData.Address, _heartbeat);

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
                if (_membersList.ContainsKey(kv.Key) == false)
                {
                    AddNewNode(kv.Key, kv.Value);
                }
                else
                {
                    var node = _membersList[kv.Key];

                    if (node.Heartbeat < kv.Value)
                    {
                        node.Heartbeat = kv.Value;
                        node.Timestamp = _timeStamp;
                        node.State = MemberState.Ok;
                    }
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
        Failed
    }
}
