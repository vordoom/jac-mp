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
        public void Update_NewNode_NodeJoinedEventRasied()
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
        public void Update_NewNode_ReflectedInCollection()
        {
            // arrange
            Node ownData = new Node(new Uri("tcp://127.0.0.1"));
            Uri knownNodeUri = new Uri("tcp://127.0.0.2");
            Uri newNodeUri = new Uri("tcp://127.0.0.3");
            var newNodes = new KeyValuePair<Uri, long>[] { new KeyValuePair<Uri, long>(newNodeUri, 5) };

            var transport = Substitute.For<IGossipTransport>();
            transport.Ping(knownNodeUri, Arg.Any<KeyValuePair<Uri, long>[]>()).Returns(newNodes);

            IStrategy strategy = new GossipStrategy(ownData, knownNodeUri, transport);

            // act
            strategy.Update();

            // assert
            Assert.IsTrue(strategy.Nodes.Any(a => a.Address == newNodeUri), "New node should be added to collection.");
        }
    }
}
