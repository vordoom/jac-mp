using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;

namespace jac.mp.gossip
{
    [TestClass]
    public class GossipStrategyTest
    {
        [TestMethod]
        public void Update_OnNewNode_NodeJoinedEventRasied()
        {
            // arrange
            Node ownData = new Node(new Uri("tcp://127.0.0.1"));
            Uri knownNodeUri = new Uri("tcp://127.0.0.2");
            Uri newNodeUri = new Uri("tcp://127.0.0.3");
            var newNodes = new KeyValuePair<Uri, long>[] { new KeyValuePair<Uri, long>(newNodeUri, 5) };

            var transport = Substitute.For<IGossipTransport>();
            transport.Ping(knownNodeUri, Arg.Any<KeyValuePair<Uri, long>[]>()).Returns(newNodes);

            IStrategy strategy = new GossipStrategy(ownData, knownNodeUri, transport);

            bool called = false;
            strategy.NodeJoined += (o, e) => { called = e.Address == newNodeUri; };

            // act
            strategy.Update();

            // assert
            Assert.IsTrue(called, "'NodeJoined' event should be raised when new node added.");
        }

        [TestMethod]
        public void Update_OnNewNode_ReflectedInCollection()
        {
            // arrange
            Node ownData = new Node(new Uri("tcp://127.0.0.1"));
            Uri knownNodeUri = new Uri("tcp://127.0.0.2");
            Uri newNodeUri = new Uri("tcp://127.0.0.3");
            var newNodes = new KeyValuePair<Uri, long>[] { 
                new KeyValuePair<Uri, long>(newNodeUri, 5), 
                new KeyValuePair<Uri, long>(knownNodeUri, 5) 
            };

            var transport = Substitute.For<IGossipTransport>();
            transport.Ping(knownNodeUri, Arg.Any<KeyValuePair<Uri, long>[]>()).Returns(newNodes);

            IStrategy strategy = new GossipStrategy(ownData, knownNodeUri, transport);

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
            Node ownData = new Node(new Uri("tcp://127.0.0.1"));
            Uri knownNodeUri = new Uri("tcp://127.0.0.2");

            var transport = Substitute.For<IGossipTransport>();
            transport
                .Ping(knownNodeUri, Arg.Any<KeyValuePair<Uri, long>[]>())
                .Returns(a => new KeyValuePair<Uri, long>[] { new KeyValuePair<Uri, long>(knownNodeUri, counter) });

            IStrategy strategy = new GossipStrategy(ownData, knownNodeUri, transport);

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
            Node ownData = new Node(new Uri("tcp://127.0.0.1"));
            Uri knownNodeUri = new Uri("tcp://127.0.0.2");

            var transport = Substitute.For<IGossipTransport>();
            transport
                .Ping(knownNodeUri, Arg.Any<KeyValuePair<Uri, long>[]>())
                .Returns(a => new KeyValuePair<Uri, long>[] { new KeyValuePair<Uri, long>(knownNodeUri, counter) });

            IStrategy strategy = new GossipStrategy(ownData, knownNodeUri, transport);
            strategy.NodeJoined += (o, e) => { nodeJoinCalled = true; };
            strategy.NodeFailed += (o, e) => { nodeFailCalled = true; };

            // act
            for (int i = 0; i < 100; i++)
            {
                counter++;
                strategy.Update();
            }

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
            Node ownData = new Node(new Uri("tcp://127.0.0.1"));
            Uri nodeOk = new Uri("tcp://127.0.0.2");
            Uri nodeFail = new Uri("tcp://127.0.0.3");

            var transport = Substitute.For<IGossipTransport>();
            transport
                .Ping(nodeOk, Arg.Any<KeyValuePair<Uri, long>[]>())
                .Returns(a => new KeyValuePair<Uri, long>[] { new KeyValuePair<Uri, long>(nodeOk, counter) });

            IStrategy strategy = new GossipStrategy(ownData, new Uri[] { nodeOk, nodeFail }, transport);
            strategy.NodeFailed += (o, e) => { nodeFailCalled = true; };

            // act
            for (int i = 0; i < 100; i++)
            {
                counter++;
                strategy.Update();
            }

            // assert
            Assert.IsTrue(nodeFailCalled, "'NodeFailed' event should be rasied when node fails.");
        }

        [TestMethod]
        public void Update_OnFailedNode_ReflectedInCollection()
        {
            // arrange
            int counter = 5;
            Node ownData = new Node(new Uri("tcp://127.0.0.1"));
            Uri nodeOk = new Uri("tcp://127.0.0.2");
            Uri nodeFail = new Uri("tcp://127.0.0.3");

            var transport = Substitute.For<IGossipTransport>();
            transport
                .Ping(nodeOk, Arg.Any<KeyValuePair<Uri, long>[]>())
                .Returns(a => new KeyValuePair<Uri, long>[] { new KeyValuePair<Uri, long>(nodeOk, counter) });

            IStrategy strategy = new GossipStrategy(ownData, new Uri[] { nodeOk, nodeFail }, transport);

            // act
            for (int i = 0; i < 100; i++)
            {
                counter++;
                strategy.Update();
            }

            // assert
            Assert.IsTrue(strategy.Nodes.Any(a => a.Address == nodeFail) == false, "Failed node should be removed from collection.");
        }

        [TestMethod]
        public void Update_OnTransportFail_ProtocolNotCrash()
        {
            // arrange
            Node ownData = new Node(new Uri("tcp://127.0.0.1"));
            Uri knownNode = new Uri("tcp://127.0.0.2");

            var transport = Substitute.For<IGossipTransport>();
            transport
                .Ping(knownNode, Arg.Any<KeyValuePair<Uri, long>[]>())
                .Returns(a => { throw new Exception(); });

            IStrategy strategy = new GossipStrategy(ownData, knownNode, transport);
            
            // act
            strategy.Update();

            // assert
            Assert.IsTrue(true, "Protocol should work on transport failure.");
        }
    }
}
