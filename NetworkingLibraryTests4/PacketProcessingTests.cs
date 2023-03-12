using NetworkingLibrary;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary.Tests
{
    [TestFixture()]
    public class PacketProcessingTests
    {
        [Test()]
        public void ProcessConstructPacketTest()
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

            byte[] data = Encoding.ASCII.GetBytes($"0/25/ACCEPT/{localSequence}/{remoteSequence}/id={clientID}/objID={obj.ObjectID}/{objType.FullName}/PROPSTART/test=testValue/anotherTest=anotherTestValue/PROPEND/");
            Packet constructPacket = new Packet("123.125.5.5", sourceIP, 28000, data, PacketType.CONSTRUCT);

            // I think the reason this fails is because of the differing assemblies between the test project and the main project. The assembly name needs to be included in the packet
            // so that the library can load the correct assembly before searching for the type

            // Act
            manager.ProcessConstructPacket(constructPacket);

            // Assert

            bool isConnectionSequenceUpdated = false;
            bool isConstructDictionaryCreatedProperly = false;
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
            if (manager.LastRemoteConstructPropertiesCreated == constructProperties)
            {
                isConstructDictionaryCreatedProperly = true;
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
    }
}
