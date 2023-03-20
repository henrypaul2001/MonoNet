using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public void StartReceivingTest()
        {
            // Arrange
            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);
            var mockSocket = new Mock<SocketWrapper>(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            string testString = "test data";

            // Setup mock
            byte[] testBuffer = new byte[1024];
            mockSocket.Setup(s => s.BeginReceiveFrom(testBuffer, 0, testBuffer.Length, SocketFlags.None, ref It.Ref<EndPoint>.IsAny, It.IsAny<AsyncCallback>(), It.IsAny<object>()))
                .Callback<byte[], int, int, SocketFlags, EndPoint, AsyncCallback, object>((buffer, offset, size, flags, ep, callback, state) =>
                {
                    // Simulate data receive
                    byte[] testData = Encoding.ASCII.GetBytes(testString);
                    Array.Copy(testData, 0, buffer, offset, testData.Length);

                    // Invoke socket callback
                    //callback.Invoke(new Mock<IAsyncResult>().Object);
                    mockSocket.Object.Socket.BeginReceiveFrom(buffer, offset, size, flags, ep, callback, state);
                });

            // Act
            manager.PacketManager.StartReceiving(mockSocket.Object, manager);
            manager.Close();
            // Assert
        }
    }
}
