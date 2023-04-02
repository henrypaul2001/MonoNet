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
            Client remoteClient = new Client("123.123.2.2", 28000, false, false, 123, manager);
            Connection testConnection = new Connection(manager.LocalClient, remoteClient, 5);

            // All 32 packets before most recent packet have been 'received'
            int testRemoteSequence = 100;

            for (int i = 0; i < 101; i++)
            {
                testConnection.AddToBuffer(i);
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
            Client remoteClient = new Client("123.123.2.2", 28000, false, false, 123, manager);
            Connection testConnection = new Connection(manager.LocalClient, remoteClient, 5);

            // All 32 packets before remote sequence have been received - apart from RemoteSequence-14, -15, -24 and -25
            int testRemoteSequence = 100;
            List<int> missingSequences = new List<int>() { testRemoteSequence - 14, testRemoteSequence - 15, testRemoteSequence - 24, testRemoteSequence - 25 };

            for (int i = 0; i < 101; i++)
            {
                if (!missingSequences.Contains(i))
                {
                    testConnection.AddToBuffer(i);
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
    }
}
