using NUnit.Framework;
using NetworkingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary.Tests
{
    [TestFixture()]
    public class NetworkManagerTests
    {
        [Test()]
        public void SendLocalObjectsTest_1Object_IsPayloadConstructedCorrectly()
        {
            // Arrange
            string destinationIP = "150.150.7.7";
            int destinationPort = 28000;
            int clientID = 567;

            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);
            TestNetworkedObject localObject1 = new TestNetworkedObject(manager, 25, new Dictionary<string, string>() { { "test1", "test1Value" }, { "otherTest1", "otherTest1Value"} });
            TestNetworkedObject localObject2 = new TestNetworkedObject(manager, 25, new Dictionary<string, string>() { { "test2", "test2Value" }, { "otherTest2", "otherTest2Value"} });
            TestNetworkedObject localObject3 = new TestNetworkedObject(manager, 25, new Dictionary<string, string>() { { "test3", "test3Value" } });

            Client fakeRemoteClient = new Client(destinationIP, destinationPort, false, false, clientID, manager);

            manager.RemoteClientsInternal.Add(fakeRemoteClient);

            Connection fakeConnection = new Connection(manager.LocalClient, fakeRemoteClient, 5);
            manager.ConnectionsInternal.Add(fakeConnection);

            string expectedPayload1 = $"id={manager.LocalClient.ID}/objID={localObject1.ObjectID}/{localObject1.GetType()}/PROPSTART/test1=test1Value/otherTest1=otherTest1Value/PROPEND/";
            string expectedPayload2 = $"id={manager.LocalClient.ID}/objID={localObject2.ObjectID}/{localObject2.GetType()}/PROPSTART/test2=test2Value/otherTest2=otherTest2Value/PROPEND/";
            string expectedPayload3 = $"id={manager.LocalClient.ID}/objID={localObject3.ObjectID}/{localObject3.GetType()}/PROPSTART/test3=test3Value/PROPEND/";

            // Act
            manager.SendLocalObjects(fakeConnection);

            // Assert
            List<string> actualPayloads = manager.PayloadsSent;
            manager.Close();
            if (actualPayloads.Contains(expectedPayload1) && actualPayloads.Contains(expectedPayload2) && actualPayloads.Contains(expectedPayload3))
            {
                Assert.Pass();
            }
            else
            {
                Assert.Fail("Not all local object payloads were constructed properly");
            }
        }

        [Test()]
        public void SendGameStateTest_IsPayloadConstructedCorrectly()
        {
            // Arrange
            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);
            TestNetworkedObject localObject = new TestNetworkedObject(manager, 25, new Dictionary<string, string>() { {"test", "test" } });
            TestNetworkedObject remoteObject = new TestNetworkedObject(manager, 25, 25);

            // Act
            manager.SendGameState();

            // Assert
            string expectedPayload = $"id={manager.LocalClient.ID}/objID={localObject.ObjectID}/VARSTART/testVariable=1/VAREND/";
            string actualPayload = manager.LastPayloadSent;
            manager.Close();
            Assert.AreEqual(expectedPayload, actualPayload);
        }

        [Test()]
        public void ClientTimeoutTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void GetClientIDsTest()
        {
            // Arrange
            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);

            Client fakeRemoteClient1 = new Client("125.125.1.1", 27000, false, false, 111, manager);
            manager.RemoteClientsInternal.Add(fakeRemoteClient1);

            Client fakeRemoteClient2 = new Client("122.122.2.2", 27000, false, false, 222, manager);
            manager.RemoteClientsInternal.Add(fakeRemoteClient2);

            Client fakeRemoteClient3 = new Client("133.133.3.3", 28000, false, false, 333, manager);
            manager.RemoteClientsInternal.Add(fakeRemoteClient3);

            List<int> expected = new List<int> { 111, 222, 333 };

            // Act
            List<int> actual = manager.GetClientIDs();
            manager.Close();

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test()]
        public void GetConnectedAddressesTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void GetPendingAddressesTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void CloseTest()
        {
            Assert.Fail();
        }
    }
}