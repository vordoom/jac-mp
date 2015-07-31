using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using log4net;
using System.Configuration;
using jac.mp.Gossip.Configuration;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace jac.mp.Gossip
{
    // todo: ping in push mode does not update heartbeat of target node
    // todo: implement WCF transport
    // todo: change callback to event
    // todo: simple console application
    // todo: enable/disabe multi-threading (remove Thread.Sleep in tests)
    // todo: emulator should be well-designed
    // todo: collect performance metrics 

    // todo: onfiguration (who should hold URI - local, remote nodes)
    // todo: should not be 'blackout' situation, when all nodes removed (no-one to ping)

    public class GossipStrategy : IStrategy
    {
        private const int UPDATING = 1;
        private const int IDLE = 0;

        #region Static members.

        /// <summary>
        /// Try to get Gossip configuration from application configuration manager.
        /// </summary>
        /// <returns>Gossip configuration.</returns>
        static GossipConfiguration GetGossipConfiguration()
        {
            var config = ConfigurationManager.GetSection(GossipConfigurationSection.ConfigurationSectionName) as GossipConfigurationSection;

            if (config == null)
                throw new ConfigurationErrorsException(String.Format("Cannot locate configuration section '{0}'", GossipConfigurationSection.ConfigurationSectionName));

            return new GossipConfiguration(config);
        } 

        #endregion

        private readonly object _syncRoot = new object();
        private readonly GossipConfiguration _configuration;
        private readonly IGossipTransport _transport;
        private readonly ConcurrentDictionary<Uri, MemberInfo> _membersList = new ConcurrentDictionary<Uri, MemberInfo>();
        private readonly Random _random;
        private readonly Uri _localUri;
        private readonly ILog _log = null;
        private long _heartbeat;
        private long _timeStamp;
        private int _state = IDLE;

        public event EventHandler<Node> NodeJoined;
        public event EventHandler<Node> NodeFailed;

        public IEnumerable<Node> Nodes
        {
            get { return _membersList.Values.Where(a => a.State == MemberState.Ok).Select(a => a.NodeData); }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public GossipStrategy(Uri[] nodes, IGossipTransport transport)
            : this(nodes, transport, GetGossipConfiguration())
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="transport"></param>
        public GossipStrategy(Uri[] nodes, IGossipTransport transport, GossipConfiguration configuration)
        {
            if (transport == null)
                throw new ArgumentNullException("transport");

            if (nodes == null)
                throw new ArgumentNullException("nodes");

            if (configuration == null)
                throw new ArgumentNullException("configuration");

            _heartbeat = 0;
            _timeStamp = 0;

            _configuration = configuration.Clone();
            _random = configuration.RandomSeed < 0 ? new Random() : new Random(configuration.RandomSeed);
            _log = LogManager.GetLogger(GetType());

            _transport = transport;
            _transport.IncomingPingCallback = OnPingRequest;

            // try to get local URI
            try
            {
                _localUri = _transport.LocalUri;
            }
            catch(Exception ex)
            {
                _log.Error("Failed to retrieve local URI value from transport.", ex);

                throw new Exception("Failed to retrieve local URI value from transport.", ex);
            }

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
            var originalValue = Interlocked.Exchange(ref _state, UPDATING);
            if (originalValue != IDLE)
            {
                _log.DebugFormat("'{0}' Another update is already running.");
                return;
            }

            try
            {
                Interlocked.Increment(ref _timeStamp);
                Interlocked.Increment(ref _heartbeat);

                // ping random nodes
                NodeInformation[] allUpdates = GetUpdates();

                lock (_syncRoot)
                {
                    var membersState = UpdateMembers(allUpdates);
                    var newMembers = membersState.Where(a => a.Value == UpdateResult.IsNew).Select(a => _membersList[a.Key].NodeData).ToArray();

                    // process not responding nodes -> mark as failed
                    var toMarkAsFail = _membersList.Where(a => a.Value.Timestamp < _timeStamp - _configuration.FailTimeout).Select(a => a.Value);
                    foreach (var v in toMarkAsFail)
                        v.State = MemberState.Failed;

                    var failedMembers = toMarkAsFail.Select(a => a.NodeData).ToArray();

                    // process nodes to remove
                    var toRemove = _membersList.Where(a => a.Value.Timestamp < _timeStamp - _configuration.RemoveTimeout).Select(a => a.Value.NodeData.Address).ToArray();
                    foreach (var uri in toRemove)
                    {
                        MemberInfo info;
                        _membersList.TryRemove(uri, out info);
                    }

                    Task.Run(() => RaiseNotifications(newMembers, failedMembers));
                }
            }
            finally
            {
                _state = IDLE;
            }
        }

        /// <summary>
        /// Ping nodes and collect updates.
        /// </summary>
        /// <returns>Collected updates from all nodes.</returns>
        private NodeInformation[] GetUpdates()
        {
            var pingNodes = GetRandomNodes();
            var localInformation = new NodeInformation() { Address = _localUri, Hearbeat = _heartbeat };
            var updatesQueue = new ConcurrentQueue<NodeInformation>();
            NodeInformation[] membersInformation = null;

            if (_configuration.InformationExchangePattern == InformationExchangePattern.Push ||
                _configuration.InformationExchangePattern == InformationExchangePattern.PushPull)
                membersInformation = GetNodesInformation();
            else
                membersInformation = new NodeInformation[0];

            // ping nodes
            foreach (var uri in pingNodes)
            {
                var result = Ping(uri, localInformation, membersInformation);
                updatesQueue.EnqueueAll(result);
            }

            // process results
            Dictionary<Uri, long> updates = new Dictionary<Uri, long>();

            while (updatesQueue.Count > 0)
            {
                NodeInformation r;
                if (updatesQueue.TryDequeue(out r) == false)
                    continue;

                if (updates.ContainsKey(r.Address) == false)
                    updates[r.Address] = r.Hearbeat;
                else if (updates[r.Address] < r.Hearbeat)
                    updates[r.Address] = r.Hearbeat;
            }

            return updates.Select(a => new NodeInformation() { Address = a.Key, Hearbeat = a.Value }).ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeUri"></param>
        private NodeInformation[] Ping(Uri nodeUri, NodeInformation localInformation, NodeInformation[] membersInformation)
        {
            _log.DebugFormat("{0} \t sending ping request to {1}", _localUri, nodeUri);

            try
            {
                var result = _transport.Ping(nodeUri, localInformation, membersInformation);

                return result ?? new NodeInformation[0];
            }
            catch (Exception ex)
            {
                _log.Debug(string.Format("Failed to ping node {0}", nodeUri), ex);

                return new NodeInformation[0];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodesInformation"></param>
        /// <returns></returns>
        private NodeInformation[] OnPingRequest(NodeInformation senderInformation, NodeInformation[] membersInformation)
        {
            if (senderInformation == null)
                throw new ArgumentNullException("senderInformation");

            if (membersInformation == null)
                throw new ArgumentNullException("nodesInformation");

            _log.DebugFormat("{0} \t received ping request from {1}", _localUri, senderInformation.Address);

            Interlocked.Increment(ref _heartbeat);

            lock (_syncRoot)
            {
                Node[] newMembers = null;

                // update sender related information
                UpdateMember(senderInformation);

                // update members if available
                if (membersInformation.Length > 0)
                {
                    var results = UpdateMembers(membersInformation);
                    newMembers = results.Where(a => a.Value == UpdateResult.IsNew).Select(a => _membersList[a.Key].NodeData).ToArray();
                }

                Task.Run(() => RaiseNotifications(newMembers, null));
            }

            if (_configuration.InformationExchangePattern == InformationExchangePattern.Pull ||
                _configuration.InformationExchangePattern == InformationExchangePattern.PushPull)
                return GetNodesInformation();
            else
                return new NodeInformation[] { new NodeInformation { Address = _localUri, Hearbeat = _heartbeat } };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        private Dictionary<Uri, UpdateResult> UpdateMembers(NodeInformation[] nodesInformation)
        {
            var results = new Dictionary<Uri, UpdateResult>();

            foreach (var n in nodesInformation)
                results[n.Address] = UpdateMember(n);

            return results;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeInformation"></param>
        private UpdateResult UpdateMember(NodeInformation nodeInformation)
        {
            if (nodeInformation.Address == _localUri)
                return UpdateResult.IsOrigianl;

            MemberInfo node;
            if (_membersList.TryGetValue(nodeInformation.Address, out node) == false)
            {
                AddNewNode(nodeInformation.Address, nodeInformation.Hearbeat);

                return UpdateResult.IsNew;
            }

            if (node.Heartbeat < nodeInformation.Hearbeat)
            {
                node.Heartbeat = nodeInformation.Hearbeat;
                node.Timestamp = _timeStamp;

                if (node.State != MemberState.Ok)
                {
                    node.State = MemberState.Ok;

                    return UpdateResult.IsNew;
                }

                return UpdateResult.IsUpdated;
            }

            return UpdateResult.IsOrigianl;
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

            _membersList.TryAdd(uri, node);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private NodeInformation[] GetNodesInformation()
        {
            return _membersList.Where(a => a.Value.State == MemberState.Ok).Select(a=> new NodeInformation() { Address = a.Key, Hearbeat = a.Value.Heartbeat }).ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Uri> GetRandomNodes()
        {
            lock (_syncRoot)
            {
                int number = _configuration.RequestsPerUpdate < _membersList.Count 
                    ? _configuration.RequestsPerUpdate 
                    : _membersList.Count;

                return _membersList.Keys.OrderBy(x => _random.Next()).Take(number).ToArray();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newNodes"></param>
        /// <param name="failedNodes"></param>
        private void RaiseNotifications(Node[] newNodes, Node[] failedNodes)
        {
            if (newNodes != null)
                foreach (var v in newNodes)
                    OnNodeJoined(v);

            if (failedNodes != null)
                foreach (var v in failedNodes)
                    OnNodeFailed(v);
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

    public enum UpdateResult
    {
        IsNew,
        IsUpdated,
        IsOrigianl
    }
}
