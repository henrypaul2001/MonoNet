using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkingLibrary.Tests
{
    [TestFixture()]
    public class DiagnosticsTests
    {
        [Test()]
        public void UpdateRTTTest_BufferDoesntExceedMaximum_And_StructIsUpdatedCorrectly()
        {
            // Arrange
            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);
            Client remoteClient = new Client("123.123.2.2", 28000, 123, manager);
            Connection testConnection = new Connection(manager.LocalClient, remoteClient, 5);
            List<Packet> TenRecentSentPackets = new List<Packet>();
            List<Packet> TenRecentReceivedPackets = new List<Packet>();

            // Act
            // Simulate sending of packets
            for (int i = 0; i < 15; i++)
            {
                Packet packetToSend = new Packet(PacketType.SYNC, i, 0, AckBitfield.Ack1, Encoding.ASCII.GetBytes("test"), remoteClient.IP, remoteClient.Port, DateTime.UtcNow);
                testConnection.PacketSent(packetToSend);
                if (i >= 4)
                {
                    TenRecentSentPackets.Add(packetToSend);
                }
            }

            // Remote client will use this bitfield to acknowledge all packets sent in test
            AckBitfield testBitfield = new AckBitfield();
            for (int i = 0; i < 33; i++)
            {
                testBitfield |= (AckBitfield)(1 << i);
            }

            // Simulate receive of packets
            for (int i = 0; i < 15; i++)
            {
                // Since there is no actual network travel, the send time of the received packet also equates to the time the packet was 'received', which means it can be used to calculate RTT
                Packet packetToReceive = new Packet(PacketType.SYNC, i, i, testBitfield, Encoding.ASCII.GetBytes("test"), manager.LocalClient.IP, manager.LocalClient.Port, DateTime.UtcNow);
                testConnection.PacketReceived(packetToReceive);
                if (i >= 4)
                {
                    TenRecentReceivedPackets.Add(packetToReceive);
                }
            }

            float actual = testConnection.DiagnosticInfo.RTT;
            manager.Close();

            float rttSum = 0;
            for (int i = 0; i < 10; i++)
            {
                rttSum += (float)(TenRecentReceivedPackets[i].SendTime - TenRecentSentPackets[i].SendTime).TotalMilliseconds;
            }
            float expected = rttSum / 10;

            // Assert - calculated RTT should be an average of the 10 most recently received RTT values
            Assert.AreEqual(expected, actual);
        }

        [Test()]
        public void UpdatePacketLossPercentageTest_StructIsUpdatedCorrectly()
        {
            // Arrange
            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);
            Client remoteClient = new Client("123.123.2.2", 28000, 123, manager);
            Connection testConnection = new Connection(manager.LocalClient, remoteClient, 5);

            // Act
            // Simulate sending of 100 packets with the first 24 being lost packets
            for (int i = 0; i < 100; i++)
            {
                Packet packetToSend = new Packet(PacketType.SYNC, i, 0, AckBitfield.Ack1, Encoding.ASCII.GetBytes("test"), remoteClient.IP, remoteClient.Port, DateTime.UtcNow);
                if (i < 24)
                {
                    packetToSend.PacketLost = true;
                }
                testConnection.PacketSent(packetToSend);
            }

            testConnection.DiagnosticInfo.UpdatePacketLossPercentage(100, testConnection.GetPacketsLostInBuffer());
            float actual = testConnection.DiagnosticInfo.PacketLossPercentage;
            manager.Close();

            // Assert
            Assert.AreEqual(24.0f, actual);
        }
    }
}
