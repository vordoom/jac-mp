using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using log4net;

namespace jac.mp.gossip
{
    // todo: next -> must send 'node' object in events
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
        private readonly ILog _log = null;
        private long _heartbeat;
        private long _timeStamp;

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
            _timeStamp = 0;
            _random = new Random();
            _ownUri = new Uri("127.0.0.1");
            _log = LogManager.GetLogger(this.GetType());

            foreach (var n in nodes)
            {
                if (n == null)
                    throw new ArgumentException("Uri must not be null.");

                _membersList.Add(n, 
                    new MemberInfo()
                    {
                        Heartbeat = 0,
                        State = MemberState.Ok,
                        Timestamp = _timeStamp
                    });
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
                    _membersList.Add(kv.Key,
                    new MemberInfo()
                    {
                        Heartbeat = kv.Value,
                        State = MemberState.Ok,
                        Timestamp = _timeStamp
                    });

                    OnNodeJoined(kv.Key);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Dictionary<Uri,long> GetMembersDictionary()
        {
            var dict = _membersList.ToDictionary(a => a.Key, a => a.Value.Heartbeat);
            dict.Add(_ownUri, _heartbeat);

            return dict;
        }


        /// <summary>
        /// 
        /// </summary>
        private void OnNodeJoined(Uri uri)
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
