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
        public void SendLocalObjectsTest()
        {
            Assert.Fail();
        }

        [Test()]
        public void SendGameStateTest_IsPayloadConstructedCorrectly()
        {
            // Arrange
            var manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);
            var localObject = new TestNetworkedObject(manager, 25, new Dictionary<string, string>() { {"test", "test" } });
            var remoteObject = new TestNetworkedObject(manager, 25, 25);

            // Act
            manager.SendGameState();

            // Assert
            string expectedPayload = $"id={manager.LocalClient.ID}/objID={localObject.ObjectID}/VARSTART/TestVariable=1/VAREND/";
            string actualPayload = manager.LastPayloadSent;
            manager.Close();
            Assert.AreEqual(expectedPayload, actualPayload);
        }

        [Test()]
        public void ConstructRemoteObjectTest()
        {
            Assert.Fail();
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