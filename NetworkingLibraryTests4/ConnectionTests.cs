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
using Moq;
using System.Threading;

namespace NetworkingLibrary.Tests
{
    [TestFixture()]
    public class ConnectionTests
    {
        [Test()]
        public void GenerateAckBitfieldTest_All32RecentPackets_IsFieldGeneratedCorrectly()
        {
            // Arrange
            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);
            Client remoteClient = new Client("123.123.2.2", 28000, 123, manager);
            Connection testConnection = new Connection(manager.LocalClient, remoteClient, 5);

            // All 32 packets before most recent packet have been 'received'
            int testRemoteSequence = 100;

            for (int i = 0; i < 101; i++)
            {
                testConnection.AddToSequenceBuffer(i);
            }

            testConnection.InternalRemoteSequence = testRemoteSequence;

            AckBitfield expected = new AckBitfield();
            for (int i = 0; i < 33; i++)
            {
                expected |= (AckBitfield)(1 << i);
            }

            // Act
            AckBitfield actual = testConnection.GenerateAckBitfield();
            manager.Close();

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test()]
        public void GenerateAckBitfieldTest_Missing_N_Minus_14_And_15_And_24_And_25_IsFieldGeneratedCorrectly()
        {
            // Arrange
            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);
            Client remoteClient = new Client("123.123.2.2", 28000, 123, manager);
            Connection testConnection = new Connection(manager.LocalClient, remoteClient, 5);

            // All 32 packets before remote sequence have been received - apart from RemoteSequence-14, -15, -24 and -25
            int testRemoteSequence = 100;
            List<int> missingSequences = new List<int>() { testRemoteSequence - 14, testRemoteSequence - 15, testRemoteSequence - 24, testRemoteSequence - 25 };

            for (int i = 0; i < 101; i++)
            {
                if (!missingSequences.Contains(i))
                {
                    testConnection.AddToSequenceBuffer(i);
                }
            }

            testConnection.InternalRemoteSequence = testRemoteSequence;

            AckBitfield expected = new AckBitfield();
            for (int i = 0; i < 33; i++)
            {
                if (i != 14 && i != 15 && i != 24 && i != 25)
                {
                    expected |= (AckBitfield)(1 << i);
                }
            }

            // Act
            AckBitfield actual = testConnection.GenerateAckBitfield();
            manager.Close();

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test()]
        public void ProcessAckBitfieldTest_AreAcknowledgedPackets_RemovedFrom_WaitingForAckDictionary()
        {
            // Arrange
            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);
            Client remoteClient = new Client("123.123.2.2", 28000, 123, manager);
            Connection testConnection = new Connection(manager.LocalClient, remoteClient, 5);

            // Create packets and pass to test connection to simulate the sending of packets
            List<int> sequencesToAcknowledge = new List<int>() { 0, 1, 2, 3, 4, 5, 10, 11, 12, 13, 14, 20, 21 };
            //List<Packet> expected = new List<Packet>();
            Dictionary<int, Packet> expected = new Dictionary<int, Packet>();
            for (int i = 0; i < 33; i++)
            {
                string payload;
                if (sequencesToAcknowledge.Contains(i))
                {
                    payload = "acknowledge me";
                }
                else
                {
                    payload = "dont acknowledge me";
                }

                Packet packetToSend = new Packet(PacketType.SYNC, i, 0, AckBitfield.Ack1, Encoding.ASCII.GetBytes(payload), remoteClient.IP, remoteClient.Port, DateTime.UtcNow);
                testConnection.PacketSent(packetToSend);

                // If packet isn't being acknowledged in this test, add it to the expected waiting for ack dictionary
                if (!sequencesToAcknowledge.Contains(i))
                {
                    expected.Add(i, packetToSend);
                }
            }

            // Create ackbitfield based on sequences that are to be acknowledged by this test
            AckBitfield testBitfield = new AckBitfield();
            for (int i = 0; i < 33; i++)
            {
                if (sequencesToAcknowledge.Contains(sequencesToAcknowledge.Last() - i))
                {
                    testBitfield |= (AckBitfield)(1 << i);
                }
            }

            // Act
            testConnection.ProcessAckBitfield(sequencesToAcknowledge.Last(), testBitfield);
            Dictionary<int, Packet> actual = testConnection.InternalPacketsWaitingForAck;
            //List<Packet> actual = testConnection.InternalWaitingPackets;
            manager.Close();

            // wait for threads to exit
            //Thread.Sleep(5 * 1000);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test()]
        public void CheckForLostPacketsTest_Wait6Seconds_AreLostPackets_RemovedFrom_WaitingList_And_AddedToLostList()
        {
            // Arrange
            int packetTimeoutTime = 5; // in seconds

            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);
            Client remoteClient = new Client("123.123.2.2", 28000, 123, manager);
            Connection testConnection = new Connection(manager.LocalClient, remoteClient, packetTimeoutTime);

            for (int i = 0; i < 10; i++)
            {
                Packet packetToSend = new Packet(PacketType.SYNC, i, 0, AckBitfield.Ack1, Encoding.ASCII.GetBytes("test"), remoteClient.IP, remoteClient.Port, DateTime.UtcNow);
                testConnection.PacketSent(packetToSend);
            }

            // Act
            Thread.Sleep((packetTimeoutTime + 1) * 1000); // sleep for timeout time
            testConnection.CheckForLostPackets();
            Dictionary<int, Packet> waitingList = testConnection.InternalPacketsWaitingForAck;
            //List<Packet> waitingList = testConnection.InternalWaitingPackets;
            List<Packet> lostPackets = testConnection.InternalLostPackets;
            manager.Close();

            // Assert
            if (waitingList.Count != 0)
            {
                Assert.Fail("Lost packets haven't been removed from waiting for ack dictionary");
            }
            else if (lostPackets.Count != 10)
            {
                Assert.Fail("Lost packets haven't been added to lost packets list");
            }
            else
            {
                Assert.Pass();
            }
        }

        [Test()]
        public void CheckForLostPacketsTest_Wait3Seconds_NoPacketsLost()
        {
            // Arrange
            int packetTimeoutTime = 5; // in seconds

            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);
            Client remoteClient = new Client("123.123.2.2", 28000, 123, manager);
            Connection testConnection = new Connection(manager.LocalClient, remoteClient, packetTimeoutTime);

            for (int i = 0; i < 10; i++)
            {
                Packet packetToSend = new Packet(PacketType.SYNC, i, 0, AckBitfield.Ack1, Encoding.ASCII.GetBytes("test"), remoteClient.IP, remoteClient.Port, DateTime.UtcNow);
                testConnection.PacketSent(packetToSend);
            }

            // Act
            Thread.Sleep((packetTimeoutTime - 2) * 1000); // sleep for timeout time - 2 seconds
            testConnection.CheckForLostPackets();
            Dictionary<int, Packet> waitingList = testConnection.InternalPacketsWaitingForAck;
            //List<Packet> waitingList = testConnection.InternalWaitingPackets;
            List<Packet> lostPackets = testConnection.InternalLostPackets;
            manager.Close();

            // Assert
            if (waitingList.Count != 10)
            {
                Assert.Fail($"Waiting packets list doesn't have required number of packets: Expected 10, Actual {waitingList.Count}");
            }
            else if (lostPackets.Count != 0)
            {
                Assert.Fail($"Lost packets list doesn't have required number of packets: Expected 0, Actual {lostPackets.Count}");
            }
            else
            {
                Assert.Pass();
            }
        }
    }
}
