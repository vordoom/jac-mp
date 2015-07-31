using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using jac.mp.Gossip.Configuration;
using System.Threading;

namespace jac.mp.Gossip
{
    // todo: test push/pull behavior

    [TestClass]
    public class GossipStrategyTest
    {
        [TestMethod]
        public void Update_OnNewNode_NodeJoinedEventRasied()
        {
            // arrange
            Uri localUri = new Uri("tcp://127.0.0.1");
            Uri knownNodeUri = new Uri("tcp://127.0.0.2");
            Uri newNodeUri = new Uri("tcp://127.0.0.3");
            var newNodes = new NodeInformation[] { new NodeInformation() { Address = newNodeUri, Hearbeat = 5 } };

            var transport = Substitute.For<IGossipTransport>();
            transport.LocalUri.Returns(localUri);
            transport.Ping(knownNodeUri, Arg.Any<NodeInformation>(), Arg.Any<NodeInformation[]>()).Returns(newNodes);

            IStrategy strategy = new GossipStrategy(new Uri[] { knownNodeUri }, transport, GossipConfiguration.DefaultConfiguration);

            bool called = false;
            strategy.NodeJoined += (o, e) => { called = e.Address == newNodeUri; };

            // act
            strategy.Update();

            Thread.Sleep(1000);

            // assert
            Assert.IsTrue(called, "'NodeJoined' event should be raised when new node added.");
        }

        [TestMethod]
        public void Update_OnNewNode_ReflectedInCollection()
        {
            // arrange
            Uri localUri = new Uri("tcp://127.0.0.1");
            Uri knownNodeUri = new Uri("tcp://127.0.0.2");
            Uri newNodeUri = new Uri("tcp://127.0.0.3");
            var newNodes = new NodeInformation[] {
                new NodeInformation() { Address = newNodeUri, Hearbeat = 5 },
                new NodeInformation() { Address = knownNodeUri, Hearbeat = 5 }
            };

            var transport = Substitute.For<IGossipTransport>();
            transport.LocalUri.Returns(localUri);
            transport.Ping(knownNodeUri, Arg.Any<NodeInformation>(), Arg.Any<NodeInformation[]>()).Returns(newNodes);

            IStrategy strategy = new GossipStrategy(new Uri[] { knownNodeUri }, transport, GossipConfiguration.DefaultConfiguration);

            // act
            strategy.Update();

            // assert
            Assert.IsTrue(strategy.Nodes.Any(a => a.Address == newNodeUri), "New node should be added to collection.");
        }

        [TestMethod]
        public void Update_AliveNode_NotStatedAsFailed()
        {
            // arrange
            int counter = 5;
            Uri localUri = new Uri("tcp://127.0.0.1");
            Uri knownNodeUri = new Uri("tcp://127.0.0.2");

            var transport = Substitute.For<IGossipTransport>();
            transport.LocalUri.Returns(localUri);
            transport
                .Ping(knownNodeUri, Arg.Any<NodeInformation>(), Arg.Any<NodeInformation[]>())
                .Returns(a => new NodeInformation[] { new NodeInformation() { Address = knownNodeUri, Hearbeat = counter } });

            IStrategy strategy = new GossipStrategy(new Uri[] { knownNodeUri }, transport, GossipConfiguration.DefaultConfiguration);

            // act
            for (int i = 0; i < 100; i++)
            {
                counter++;
                strategy.Update();
            }

            // assert
            Assert.IsTrue(strategy.Nodes.Any(a => a.Address == knownNodeUri), "Alive node should not be removed from collection.");
        }

        [TestMethod]
        public void Update_NoNodesAddOrFail_EventsAreNotRaised()
        {
            // arrange
            int counter = 5;
            bool nodeJoinCalled = false;
            bool nodeFailCalled = false;
            Uri localUri = new Uri("tcp://127.0.0.1");
            Uri knownNodeUri = new Uri("tcp://127.0.0.2");

            var transport = Substitute.For<IGossipTransport>();
            transport.LocalUri.Returns(localUri);
            transport
                .Ping(knownNodeUri, Arg.Any<NodeInformation>(), Arg.Any<NodeInformation[]>())
                .Returns(a => new NodeInformation[] { new NodeInformation() { Address = knownNodeUri, Hearbeat = counter } });

            IStrategy strategy = new GossipStrategy(new Uri[] { knownNodeUri }, transport, GossipConfiguration.DefaultConfiguration);
            strategy.NodeJoined += (o, e) => { nodeJoinCalled = true; };
            strategy.NodeFailed += (o, e) => { nodeFailCalled = true; };

            // act
            for (int i = 0; i < 100; i++)
            {
                counter++;
                strategy.Update();
            }

            Thread.Sleep(1000);

            // assert
            Assert.IsTrue(nodeJoinCalled == false, "'NodeJoined' event should not be rasied when no new nodes.");
            Assert.IsTrue(nodeFailCalled == false, "'NodeFailed' event should not be rasied when no fail nodes.");
        }

        [TestMethod]
        public void Update_OnFailedNode_NodeFailedEventRaised()
        {
            // arrange
            int counter = 5;
            bool nodeFailCalled = false;
            Uri localUri = new Uri("tcp://127.0.0.1");
            Uri nodeOk = new Uri("tcp://127.0.0.2");
            Uri nodeFail = new Uri("tcp://127.0.0.3");

            var transport = Substitute.For<IGossipTransport>();
            transport.LocalUri.Returns(localUri);
            transport
                .Ping(nodeOk, Arg.Any<NodeInformation>(), Arg.Any<NodeInformation[]>())
                .Returns(a => new NodeInformation[] { new NodeInformation() { Address = nodeOk, Hearbeat = counter } });

            IStrategy strategy = new GossipStrategy(new Uri[] { nodeOk, nodeFail }, transport, GossipConfiguration.DefaultConfiguration);
            strategy.NodeFailed += (o, e) => { nodeFailCalled = true; };

            // act
            for (int i = 0; i < 100; i++)
            {
                counter++;
                strategy.Update();
            }

            Thread.Sleep(1000);

            // assert
            Assert.IsTrue(nodeFailCalled, "'NodeFailed' event should be rasied when node fails.");
        }

        [TestMethod]
        public void Update_OnFailedNode_ReflectedInCollection()
        {
            // arrange
            int counter = 5;
            Uri localUri = new Uri("tcp://127.0.0.1");
            Uri nodeOk = new Uri("tcp://127.0.0.2");
            Uri nodeFail = new Uri("tcp://127.0.0.3");

            var transport = Substitute.For<IGossipTransport>();
            transport.LocalUri.Returns(localUri);
            transport
                .Ping(nodeOk, Arg.Any<NodeInformation>(), Arg.Any<NodeInformation[]>())
                .Returns(a => new NodeInformation[] { new NodeInformation() { Address = nodeOk, Hearbeat = counter } });

            IStrategy strategy = new GossipStrategy(new Uri[] { nodeOk, nodeFail }, transport, GossipConfiguration.DefaultConfiguration);

            // act
            for (int i = 0; i < 100; i++)
            {
                counter++;
                strategy.Update();
            }

            // assert
            Assert.IsFalse(strategy.Nodes.Any(a => a.Address == nodeFail), "Failed node should be removed from collection.");
        }

        [TestMethod]
        public void Update_OnTransportFail_ProtocolNotCrash()
        {
            // arrange
            Uri localUri = new Uri("tcp://127.0.0.1");
            Uri knownNode = new Uri("tcp://127.0.0.2");

            var transport = Substitute.For<IGossipTransport>();
            transport.LocalUri.Returns(localUri);
            transport
                .Ping(knownNode, Arg.Any<NodeInformation>(), Arg.Any<NodeInformation[]>())
                .Returns(a => { throw new Exception(); });

            IStrategy strategy = new GossipStrategy(new Uri[] { knownNode }, transport, GossipConfiguration.DefaultConfiguration);

            // act
            strategy.Update();

            // assert
            Assert.IsTrue(true, "Protocol should work on transport failure.");
        }

        [TestMethod]
        public void PingRequest_OnNewNode_NodeJoinedEventRasied()
        {
            // arrange
            Uri localUri = new Uri("tcp://127.0.0.1");
            Uri knownNodeUri = new Uri("tcp://127.0.0.2");
            Uri newNodeUri = new Uri("tcp://127.0.0.3");
            var updateInformation = new NodeInformation[] {
                new NodeInformation() { Address = newNodeUri, Hearbeat = 5 },
                new NodeInformation() { Address = localUri, Hearbeat = 5 }
            };

            var transport = Substitute.For<IGossipTransport>();
            transport.LocalUri.Returns(localUri);

            IStrategy strategy = new GossipStrategy(new Uri[] { knownNodeUri }, transport, GossipConfiguration.DefaultConfiguration);

            bool called = false;
            strategy.NodeJoined += (o, e) => { called = e.Address == newNodeUri; };

            // act
            transport.IncomingPingCallback(new NodeInformation() { Address = knownNodeUri, Hearbeat = 6 }, updateInformation);

            Thread.Sleep(1000);

            // assert
            Assert.IsTrue(called, "'NodeJoined' event should be raised when new node added.");
        }

        [TestMethod]
        public void PingRequest_OnNewNode_ReflectedInCollection()
        {
            // arrange
            Uri localUri = new Uri("tcp://127.0.0.1");
            Uri knownNodeUri = new Uri("tcp://127.0.0.2");
            Uri newNodeUri = new Uri("tcp://127.0.0.3");
            var updateInformation = new NodeInformation[] {
                new NodeInformation() { Address = newNodeUri, Hearbeat = 5 },
                new NodeInformation() { Address = localUri, Hearbeat = 5 }
            };

            var transport = Substitute.For<IGossipTransport>();
            transport.LocalUri.Returns(localUri);

            IStrategy strategy = new GossipStrategy(new Uri[] { knownNodeUri }, transport, GossipConfiguration.DefaultConfiguration);

            // act
            transport.IncomingPingCallback(new NodeInformation() { Address = knownNodeUri, Hearbeat = 6 }, updateInformation);

            // assert
            Assert.IsTrue(strategy.Nodes.Any(a => a.Address == newNodeUri), "New node should be added to collection.");
        }

        [TestMethod]
        public void PingRequest_AliveNode_NotStatedAsFailed()
        {
            // arrange
            int counter = 5;
            Uri localUri = new Uri("tcp://127.0.0.1");
            Uri node1 = new Uri("tcp://127.0.0.2");
            Uri node2 = new Uri("tcp://127.0.0.3");

            var transport = Substitute.For<IGossipTransport>();
            transport.LocalUri.Returns(localUri);
            transport
                .Ping(Arg.Any<Uri>(), Arg.Any<NodeInformation>(), Arg.Any<NodeInformation[]>())
                .Returns(a => new NodeInformation[] { });

            IStrategy strategy = new GossipStrategy(new Uri[] { node1, node2 }, transport, GossipConfiguration.DefaultConfiguration);

            // act
            for (int i = 0; i < 100; i++)
            {
                counter++;

                strategy.Update();
                transport.IncomingPingCallback(
                    new NodeInformation() { Address = node1, Hearbeat = counter },
                    new NodeInformation[] { new NodeInformation() { Address = node2, Hearbeat = counter } }
                );
            }

            // assert
            Assert.IsTrue(strategy.Nodes.Any(a => a.Address == node2), "Alive node should not be removed from collection.");
        }

        [TestMethod]
        public void PingRequest_NoNodesAddOrFail_EventsAreNotRaised()
        {
            // arrange
            int counter = 5;
            bool nodeJoinCalled = false;
            bool nodeFailCalled = false;
            Uri localUri = new Uri("tcp://127.0.0.1");
            Uri node1 = new Uri("tcp://127.0.0.2");
            Uri node2 = new Uri("tcp://127.0.0.3");

            var transport = Substitute.For<IGossipTransport>();
            transport.LocalUri.Returns(localUri);
            transport
                .Ping(Arg.Any<Uri>(), Arg.Any<NodeInformation>(), Arg.Any<NodeInformation[]>())
                .Returns(a => new NodeInformation[] { });

            IStrategy strategy = new GossipStrategy(new Uri[] { node1, node2 }, transport, GossipConfiguration.DefaultConfiguration);
            strategy.NodeJoined += (o, e) => { nodeJoinCalled = true; };
            strategy.NodeFailed += (o, e) => { nodeFailCalled = true; };

            // act
            for (int i = 0; i < 100; i++)
            {
                counter++;

                strategy.Update();
                transport.IncomingPingCallback(
                    new NodeInformation() { Address = node1, Hearbeat = counter },
                    new NodeInformation[] { new NodeInformation() { Address = node2, Hearbeat = counter } }
                );
            }

            Thread.Sleep(1000);

            // assert
            Assert.IsTrue(nodeJoinCalled == false, "'NodeJoined' event should not be rasied when no new nodes.");
            Assert.IsTrue(nodeFailCalled == false, "'NodeFailed' event should not be rasied when no fail nodes.");
        }
    }
}
