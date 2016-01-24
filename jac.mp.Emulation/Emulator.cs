using jac.mp.Gossip;
using jac.mp.Gossip.Configuration;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jac.mp.Emulation
{
    public class Emulator: IEmulationEnvironment
    {
        private const int randomSeed = 14;
        private const int requestsPerUpdate = 2;
        private const InformationExchangePattern informationExchangePattern = InformationExchangePattern.Pull;
        private const int numOfIterations = 100;
        private const int numOfNodes = 100;
        private const int numOfFailedNodes = 1;
        private const double networkFailureProbability = 0;

        private Random random = new Random(randomSeed);
        private Dictionary<Uri, IEmulationTransport> transports = new Dictionary<Uri, IEmulationTransport>();
        private List<Uri> nodes = new List<Uri>();
        private List<Uri> failedNodes = new List<Uri>();
        private Dictionary<Uri, IStrategy> strategies = new Dictionary<Uri, IStrategy>();
        private ILog _log;
        private int nodesCounter;

        public Dictionary<Uri, IEmulationTransport> Transports
        {
            get { return transports; }
        }

        Emulator()
        {
            _log = LogManager.GetLogger(this.GetType());

            var list = new List<Uri>();
            for (int i = 0; i < numOfNodes; i++)
                list.Add(new Uri("tcp://127.0.0." + i.ToString()));

            nodes = list.OrderBy(a => random.Next()).ToList();

            var config = GossipConfiguration.DefaultConfiguration;
            config.RequestsPerUpdate = requestsPerUpdate;
            config.InformationExchangePattern = informationExchangePattern;

            for (int i = 0; i < numOfNodes; i++)
            {
                var transport = new GossipEmulatorTransport(nodes[i], this);
                config.RandomSeed = randomSeed + i;

                GossipStrategy strat = null;
                if (i == 0)
                    strat = new GossipStrategy(new Uri[] { }, transport, config);
                else
                    strat = new GossipStrategy(new Uri[] { nodes[0] }, transport, config);

                strat.NodeJoined += s_NodeJoined;
                strat.NodeFailed += s_NodeFailed;

                transports.Add(nodes[i], transport);
                strategies.Add(nodes[i], strat);
            }
        }

        void Start()
        {
            bool resolve = true;

            for (int i = 0; i < numOfIterations; i++)
            {
                foreach (var v in strategies.Where(a => transports.Where(b => b.Value.Fail).Select(b => b.Key).Contains(a.Key) == false).OrderBy(a => random.Next()))
                {
                    if (failedNodes.Contains(v.Key))
                        continue;

                    v.Value.Update();
                }

                if (resolve)
                {
                    var sum = strategies.Where(a => failedNodes.Contains(a.Key) == false).Sum(a => a.Value.Nodes.Count());
                    
                    if (sum == strategies.Count * (strategies.Count - 1))
                    {
                        var r = strategies.All(
                            x => nodes
                                .Where(a => a != x.Key)
                                .All(a => x.Value.Nodes.Any(b => b.Address == a))
                        );

                        if (r == true)
                        {
                            _log.DebugFormat("All nodes reported full membership list on iteration '{0}'", i);
                            resolve = false;

                            failedNodes = strategies.Keys.OrderBy(x => random.Next()).Take(numOfFailedNodes).ToList();
                            foreach (var v in failedNodes)
                            {
                                _log.DebugFormat("Node '{0}' is failed", v);
                                transports[v].Fail = true;
                            }
                        }
                    }
                }
                else
                {
                    var sum = strategies.Where(a => failedNodes.Contains(a.Key) == false).Sum(a => a.Value.Nodes.Count());

                    if (sum == (strategies.Count - numOfFailedNodes) * (strategies.Count - numOfFailedNodes - 1))
                    {
                        var r = strategies.Where(a => transports.Where(b => b.Value.Fail).Select(b => b.Key).Contains(a.Key) == false).All(
                            x => nodes.Where(a => transports.Where(b => b.Value.Fail).Select(b => b.Key).Contains(a) == false)
                                .Where(a => a != x.Key)
                                .All(a => x.Value.Nodes.Any(b => b.Address == a))
                        );

                        if (r == true)
                        {
                            _log.DebugFormat("All nodes detected failed one at iteration '{0}'", i);

                            break;
                        }
                    }
                }
            }
        }

        void s_NodeFailed(object sender, Node e)
        {
            nodesCounter--;
        }

        void s_NodeJoined(object sender, Node e)
        {
            nodesCounter++;
        }

        static void Main()
        {
            var r = new Emulator();
            r.Start();
        }

        public bool RandomFailure()
        {
            return random.Next(100) < networkFailureProbability;
        }
    }

    public class GossipEmulatorTransport : IGossipTransport, IEmulationTransport
    {
        IEmulationEnvironment _env;
        PingRequestHandler _pingRequestHandler = null;

        public Uri LocalUri
        {
            get;
            private set;
        }

        public GossipEmulatorTransport(Uri localUri, IEmulationEnvironment env)
        {
            LocalUri = localUri;
            _env = env;
        }

        public NodeInformation[] Ping(Uri targetUri, NodeInformation[] membersInformation)
        {
            if (Fail)
                throw new Exception(string.Format("Node {0} is failed", LocalUri));

            if (_env.RandomFailure())
                throw new Exception("Network exception (local)");

            var transport = _env.Transports[targetUri] as GossipEmulatorTransport;

            return transport.IncomingPingCallback(transport.LocalUri, membersInformation);
        }

        public bool Fail { get; set; }

        public PingRequestHandler IncomingPingCallback
        {
            get
            {
                if (Fail)
                    throw new Exception(string.Format("Node {0} is failed", LocalUri));

                if (_env.RandomFailure())
                    throw new Exception("Network exception (remote)");

                return _pingRequestHandler;
            }
            set
            {
                _pingRequestHandler = value;
            }
        }
    }

    public interface IEmulationTransport
    {
        bool Fail { get; set; }
    }

    public interface IEmulationEnvironment
    {
        Dictionary<Uri, IEmulationTransport> Transports { get; }
        bool RandomFailure();
    }

}
