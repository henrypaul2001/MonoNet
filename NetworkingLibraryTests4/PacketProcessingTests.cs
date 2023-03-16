using NetworkingLibrary;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace NetworkingLibrary.Tests
{
    [TestFixture()]
    public class PacketProcessingTests
    {
        [Test()]
        public void ProcessConstructPacketTest_IsRemoteObjectConstructed_And_IsConnectionUpdated_And_IsConstructDictionaryCreatedProperly()
        {
            // Arrange
            string sourceIP = "150.150.7.7";
            int sourcePort = 28000;
            int localSequence = 1;
            int remoteSequence = 1;
            int clientID = 567;

            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);

            Client fakeRemoteClient = new Client(sourceIP, sourcePort, false, false, clientID, manager);

            manager.RemoteClientsInternal.Add(fakeRemoteClient);

            Connection fakeConnection = new Connection(manager.LocalClient, fakeRemoteClient, 5);
            manager.ConnectionsInternal.Add(fakeConnection);

            Dictionary<string, string> constructProperties = new Dictionary<string, string>() { { "test", "testValue" }, { "anotherTest", "anotherTestValue" } };

            TestNetworkedObject obj = new TestNetworkedObject(manager, clientID, constructProperties);
            Type objType = obj.GetType();

            byte[] data = Encoding.ASCII.GetBytes($"0/25/CONSTRUCT/{localSequence}/{remoteSequence}/id={clientID}/objID={obj.ObjectID}/{objType.FullName}/PROPSTART/test=testValue/anotherTest=anotherTestValue/PROPEND/");
            Packet constructPacket = new Packet(PacketType.CONSTRUCT, localSequence, remoteSequence, AckBitfield.Ack1, sourceIP, sourcePort, data);

            // Act
            manager.ProcessConstructPacket(constructPacket);

            // Assert

            bool isConnectionSequenceUpdated = false;
            bool isConstructDictionaryCreatedProperly = true;
            bool isConstructRemoteObjectCalled = false;

            // Check if connection sequence gets updated
            foreach (Connection connection in manager.ConnectionsInternal)
            {
                if (connection.RemoteClientID == clientID)
                {
                    if (connection.RemoteSequence == localSequence)
                    {
                        isConnectionSequenceUpdated = true;
                    }
                }
            }

            // Is the properties dictionary created properly?
            foreach (KeyValuePair<string, string> pair in manager.LastRemoteConstructPropertiesCreated)
            {
                if (!constructProperties.Contains(pair))
                {
                    isConstructDictionaryCreatedProperly = false;
                }
            }

            // Does the ConstructRemoteObject method get called?
            if (manager.ConstructRemoteObjectCalls < 1)
            {
                manager.Close();
                Assert.Fail("ConstructRemoteObject doesn't get called enough times");
            }
            else if (manager.ConstructRemoteObjectCalls > 1)
            {
                manager.Close();
                Assert.Fail("ConstructRemoteObject gets called too many times");
            }
            else if (manager.ConstructRemoteObjectCalls == 1)
            {
                isConstructRemoteObjectCalled = true;
            }

            if (!isConnectionSequenceUpdated)
            {
                manager.Close();
                Assert.Fail("Connection sequence number is not updated when construct packet is processed");
            }
            else if (!isConstructDictionaryCreatedProperly)
            {
                manager.Close();
                Assert.Fail("Remote object construction dictionary is not created properly when processing construct packet");
            }
            else if (isConstructDictionaryCreatedProperly && isConnectionSequenceUpdated && isConstructRemoteObjectCalled)
            {
                manager.Close();
                Assert.Pass();
            }
        }

        [Test()]
        public void ProcessSyncPacketTest_IsNetworkedVariableUpdatedCorrectly()
        {
            // Arrange
            string sourceIP = "150.150.7.7";
            int sourcePort = 28000;
            int localSequence = 1;
            int remoteSequence = 1;
            int clientID = 567;
            int objID = 250;

            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);

            Client fakeRemoteClient = new Client(sourceIP, sourcePort, false, false, clientID, manager);

            manager.RemoteClientsInternal.Add(fakeRemoteClient);

            Connection fakeConnection = new Connection(manager.LocalClient, fakeRemoteClient, 5);
            manager.ConnectionsInternal.Add(fakeConnection);

            TestNetworkedObject obj = new TestNetworkedObject(manager, clientID, objID);
            Type objType = obj.GetType();

            byte[] data = Encoding.ASCII.GetBytes($"/25/SYNC/{localSequence}/{remoteSequence}/id={clientID}/objID={objID}/VARSTART/testVariable=2/VAREND/");
            Packet syncPacket = new Packet(PacketType.SYNC, localSequence, remoteSequence, AckBitfield.Ack1, sourceIP, sourcePort, data);

            // Act
            manager.ProcessSyncPacket(syncPacket);

            // Assert
            object remoteObj = manager.GetNetworkedObjectFromClientAndObjectID(clientID, objID);
            FieldInfo field = manager.GetNetworkedObjectFromClientAndObjectID(clientID, objID).GetType().GetField("testVariable");
            if ((int)field.GetValue(remoteObj) == 2)
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail("testVariable was not updated to sync value of '2'");
            }
        }
    }
}
