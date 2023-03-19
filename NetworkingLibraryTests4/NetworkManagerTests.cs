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
        public void NetworkManagerTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void UpdateTest()
        {
            Assert.Fail();
        }

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
        public void ConnectLocalClientToHostTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void HandleConnectionRequestTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void HandleConnectionAcceptTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void ConnectionEstablishedTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void ClientDisconnectTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void ClientTimeoutTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void GetClientIDsTest()
        {
            Assert.Fail();
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