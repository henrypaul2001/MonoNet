using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary.Tests
{
    [TestFixture()]
    public class PacketManagerTests
    {
        [Test()]
        public void ReceiveCallbackTest_RoguePacketReceived()
        {
            // Arrange
            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);
            var mockSocket = new Mock<SocketWrapper>(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var mockSenderSocket = new Mock<SocketWrapper>(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            string testString = "roguepacketdata/";

            // Setup mock
            byte[] testBuffer = new byte[1024];
            mockSocket.Setup(s => s.BeginReceiveFrom(testBuffer, 0, testBuffer.Length, SocketFlags.None, It.IsAny<EndPoint>(), It.IsAny<AsyncCallback>(), It.IsAny<object>()))
                .Callback<byte[], int, int, SocketFlags, EndPoint, AsyncCallback, object>((buffer, offset, size, flags, ep, callback, state) =>
                {
                    // Setup mock async result
                    var mockResult = new Mock<IAsyncResult>();
                    mockResult.SetupGet(r => r.CompletedSynchronously).Returns(true);
                    mockResult.Setup(r => r.AsyncState).Returns(mockSenderSocket.Object);

                    // Simulate data receive
                    byte[] testData = Encoding.ASCII.GetBytes(testString);
                    Array.Copy(testData, 0, buffer, offset, testData.Length);

                    // Invoke socket callback with mocked IAsyncResult object
                    callback.Invoke(mockResult.Object);
                });

            // Act
            manager.PacketManager.StartReceiving(mockSocket.Object, manager);
            int packetsIgnored = manager.PacketManager.PacketsIgnored;
            manager.Close();

            // Assert
            if (packetsIgnored == 1)
            {
                Assert.Pass();
            }
            else if (packetsIgnored < 1)
            {
                Assert.Fail("Rogue packet wasn't ignored");
            }
            else if (packetsIgnored > 1)
            {
                Assert.Fail("Too many packets ignored");
            }
        }

        [Test()]
        public void ReceiveCallbackTest_RoguePacketReceived_And_GamePacketReceived()
        {
            // Arrange
            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);
            var mockSocket = new Mock<SocketWrapper>(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var mockSenderSocket = new Mock<SocketWrapper>(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            string testString = "roguepacketdata/";

            // Setup mock
            byte[] testBuffer = new byte[1024];
            mockSocket.Setup(s => s.BeginReceiveFrom(testBuffer, 0, testBuffer.Length, SocketFlags.None, It.IsAny<EndPoint>(), It.IsAny<AsyncCallback>(), It.IsAny<object>()))
                .Callback<byte[], int, int, SocketFlags, EndPoint, AsyncCallback, object>((buffer, offset, size, flags, ep, callback, state) =>
                {
                    // Setup mock async result
                    var mockResult = new Mock<IAsyncResult>();
                    mockResult.SetupGet(r => r.CompletedSynchronously).Returns(true);
                    mockResult.Setup(r => r.AsyncState).Returns(mockSenderSocket.Object);

                    // Simulate data receive
                    byte[] testData = Encoding.ASCII.GetBytes(testString);
                    Array.Copy(testData, 0, buffer, offset, testData.Length);

                    // Invoke socket callback with mocked IAsyncResult object
                    callback.Invoke(mockResult.Object);
                });

            // Act
            manager.PacketManager.StartReceiving(mockSocket.Object, manager);

            testString = $"0/25/REQUEST/{manager.LocalClient.ID}/{manager.LocalClient.IsHost}/{manager.LocalClient.IsServer}";

            // Setup mock
            testBuffer = new byte[1024];
            mockSocket.Setup(s => s.BeginReceiveFrom(testBuffer, 0, testBuffer.Length, SocketFlags.None, It.IsAny<EndPoint>(), It.IsAny<AsyncCallback>(), It.IsAny<object>()))
                .Callback<byte[], int, int, SocketFlags, EndPoint, AsyncCallback, object>((buffer, offset, size, flags, ep, callback, state) =>
                {
                    // Setup mock async result
                    var mockResult = new Mock<IAsyncResult>();
                    mockResult.SetupGet(r => r.CompletedSynchronously).Returns(true);
                    mockResult.Setup(r => r.AsyncState).Returns(mockSenderSocket.Object);

                    // Simulate data receive
                    byte[] testData = Encoding.ASCII.GetBytes(testString);
                    Array.Copy(testData, 0, buffer, offset, testData.Length);

                    // Invoke socket callback with mocked IAsyncResult object
                    callback.Invoke(mockResult.Object);
                });

            manager.PacketManager.StartReceiving(mockSocket.Object, manager);

            int packetsIgnored = manager.PacketManager.PacketsIgnored;
            int packetsProcessed = manager.PacketManager.PacketsProcessed;
            manager.Close();

            // Assert
            if (packetsIgnored == 1 && packetsProcessed == 1)
            {
                Assert.Pass();
            }
            else if (packetsIgnored < 1)
            {
                Assert.Fail("Rogue packet wasn't ignored");
            }
            else if (packetsIgnored > 1)
            {
                Assert.Fail("Too many packets ignored");
            }
            else if (packetsProcessed < 1 )
            {
                Assert.Fail("Game packet was ignored");
            }
            else if (packetsProcessed > 2)
            {
                Assert.Fail("Too many packets were processed");
            }
        }

        [Test()]
        public void SendPacketTest_IsBeginSendToCalled_OnSocket_WithCorrectParameters()
        {
            // Arrange
            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);
            var mockSocket = new Mock<SocketWrapper>(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            string testString = "SendTestData";
            byte[] testData = Encoding.ASCII.GetBytes(testString);

            Packet packet = new Packet("125.125.2.2", manager.LocalClient.IP, 28500, testData, PacketType.REQUEST);

            // Act
            manager.PacketManager.SendPacket(packet, mockSocket.Object);
            manager.Close();

            // Assert
            mockSocket.Verify(s => s.BeginSendTo(
                packet.Data,
                0,
                packet.Data.Length,
                It.IsAny<SocketFlags>(),
                new IPEndPoint(IPAddress.Parse(packet.IPDestination), packet.PortDestination),
                It.IsAny<AsyncCallback>(),
                It.IsAny<object>()
                ));
        }

        [Test()]
        public void ConstructPacketFromByteArrayTest_ByteArrayDoesntContainCorrectSplitCharacter()
        {
            // Arrange

            // Byte array doesn't contain ('/') character which is used to seperate the data inside the packet
            string testString = "iamsillybecauseidontcontainanyseperationcharactersawjeezohnoohdear";
            byte[] testData = Encoding.ASCII.GetBytes(testString);

            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);

            // Act
            string status = manager.PacketManager.ConstructAndProcessPacketFromByteArray(testData, "123.123.5.5", 28000);
            manager.Close();

            // Assert
            Assert.AreEqual(status, "FAIL - DATA DOES NOT CONTAIN SEPERATION CHARACTER");
        }

        [Test()]
        public void ConstructPacketFromByteArrayTest_CorrectFormatRequestPacket_IsConnectionRequestCalled_And_IsCorrectPacketConstructed()
        {
            // Arrange
            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);

            string testString = $"0/25/REQUEST/{manager.LocalClient.ID}/{manager.LocalClient.IsHost}/{manager.LocalClient.IsServer}";
            byte[] testData = Encoding.ASCII.GetBytes(testString);

            // Act
            manager.PacketManager.ConstructAndProcessPacketFromByteArray(testData, "123.123.5.5", 28000);
            int handleConnectRequestCalls = manager.HandleConnctionRequestCalls;
            Packet lastPacketConstructed = manager.PacketManager.LastPacketConstructed;
            manager.Close();

            // Assert
            if (lastPacketConstructed.PacketType != PacketType.REQUEST)
            {
                Assert.Fail("Request packet wasn't constructd");
            }
            else
            {
                Assert.AreEqual(1, handleConnectRequestCalls);
            }
        }
    }
}
