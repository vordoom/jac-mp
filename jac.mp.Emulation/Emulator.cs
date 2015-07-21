using jac.mp.Gossip;
using jac.mp.Gossip.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jac.mp.Emulation
{
    public class Emulator
    {
        // num of nodes
        // failure probability
        // num of failed nodes
        // num of iterations


        //nodes.Where(a=>a.Address != nodes[i].Address).OrderBy(a => random.Next()).Select(a=> a.Address).ToArray(), 

        private const int randomSeed = 10;
        private int numOfIterations = 100;
        private int numOfNodes = 500;
        private int numOfFailedNodes = 1;
        private double networkFailureProbability = 0;
        private Random random = new Random(randomSeed);
        private Dictionary<Uri, IEmulatorTransport> transports = new Dictionary<Uri, IEmulatorTransport>();
        private List<Uri> nodes = new List<Uri>();
        private Dictionary<Uri, IStrategy> strategies = new Dictionary<Uri, IStrategy>();

        Emulator()
        {
            var list = new List<Uri>();
            for (int i = 0; i < numOfNodes; i++)
                list.Add(new Uri("tcp://127.0.0." + i.ToString()));

            nodes = list.OrderBy(a => random.Next()).ToList();

            var config = GossipConfiguration.DefaultConfiguration;
            config.RandomSeed = randomSeed;

            for (int i = 0; i < numOfNodes; i++ )
            {
                var transport = new GossipEmulatorTransport(nodes[i], transports);

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
            for (int i = 0; i < numOfIterations; i++)
            {
                foreach (var v in strategies.Values)
                    v.Update();

                var r = strategies.All(
                    x => nodes
                        .Where(a => a != x.Key)
                        .All(a => x.Value.Nodes.Any(b => b.Address == a))
                );

                if (r == true)
                    Debug.WriteLine("All nodes on iteration {0}", i);
            }
        }

        void s_NodeFailed(object sender, Node e)
        {
            throw new NotImplementedException();
        }

        void s_NodeJoined(object sender, Node e)
        {

        }

        static void Main()
        {
            var r = new Emulator();
            r.Start();
        }
    }

    public class GossipEmulatorTransport : IGossipTransport, IEmulatorTransport
    {
        Dictionary<Uri, IEmulatorTransport> _transports;
        bool _fail;
        PingRequestHandler _pingRequestHandler = null;

        public Uri LocalUri
        {
            get;
            private set;
        }

        public GossipEmulatorTransport(Uri localUri, Dictionary<Uri, IEmulatorTransport> transports)
        {
            LocalUri = localUri;
            _transports = transports;
        }

        public KeyValuePair<Uri, long>[] Ping(Uri nodeUri, KeyValuePair<Uri, long>[] nodesInformation)
        {
            if(_fail)
                throw new Exception("Emulator network exception");

            var transport = _transports[nodeUri] as GossipEmulatorTransport;

            return transport.IncomingPingRequestHandler(nodesInformation);
        }

        public void Fail()
        {
            _fail = true;
        }

        public PingRequestHandler IncomingPingRequestHandler
        {
            get
            {
                if(_fail)
                    throw new Exception("Emulator network exception");

                return _pingRequestHandler;
            }
            set
            {
                _pingRequestHandler = value;
            }
        }
    }

    public interface IEmulatorTransport
    {
        void Fail();
    }


}
