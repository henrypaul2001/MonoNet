using NetworkingLibrary.Tests;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkingLibrary;
using System.Runtime.CompilerServices;
using System.Management.Instrumentation;
using System.Diagnostics;

namespace NetworkingLibrary.Tests
{
    [TestFixture()]
    public class ConnectionEstablishmentTests
    {
        [Test()]
        public void ConnectLocalClientToHostTest_IsConnectionRequestConstructedProperly()
        {
            // Arrange
            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);
            string expected = $"0/25/REQUEST/id={manager.LocalClient.ID}/isHost={manager.LocalClient.IsHost}/isServer={manager.LocalClient.IsServer}";

            // Act
            manager.ConnectLocalClientToHost("100.100.2.3", 27000);
            byte[] data = manager.LastLocalClientConnectionRequest;
            string actual = Encoding.ASCII.GetString(data);
            manager.Close();

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test()]
        public void HandleConnectionRequestTest_IsClientAddedToPendingList()
        {
            // Arrange
            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 27000);
            byte[] data = Encoding.ASCII.GetBytes("0/25/REQUEST/id=123/isHost=false/isServer=false");
            Packet request = new Packet(PacketType.CONNECT, "123.123.1.2", 27000, data);

            // Act
            manager.HandleConnectionRequest(request);
            List<Client> pendingClients = manager.PendingClients;

            // Add decoy client
            pendingClients.Add(new Client("125.125.3.6", 28000, true, false, 567, manager));

            manager.Close();

            // Assert
            foreach (Client client in pendingClients)
            {
                if (client.ID == 123 && client.IsHost == false && client.IsServer == false) {
                    Assert.Pass();
                }
                else
                {
                    Assert.Fail("Client not found in pending list");
                }
            }
        }

        [Test()]
        public void HandleConnectionAcceptTest_LocalClientIsReceiverOfInitialRequest_And_NoConnectionsToCopy_And_ConnectionEstablishedSuccesfully()
        {
            // Arrange
            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 28000);
            List<Client> pendingClients = new List<Client>();
            Client pendingClient = new Client("123.123.1.2", 27000, true, false, 567, manager);
            pendingClients.Add(pendingClient);
            manager.PendingClientsInternal = pendingClients;

            byte[] data = Encoding.ASCII.GetBytes($"0/25/ACCEPT/id=567/isHost=true/isServer=false/connectionNum=0/END");
            Packet acceptPacket = new Packet("123.125.5.5", pendingClients[0].IP, 28000, data, PacketType.ACCEPT);

            // Act
            manager.HandleConnectionAccept(acceptPacket);

            List<Client> remoteClients = manager.RemoteClients;
            pendingClients = manager.PendingClients;

            // Assert
            bool removedFromPending = true;
            bool addedToRemote = false;

            // Check if client has been removed from pending list
            foreach (Client client in pendingClients)
            {
                if (client.ID == pendingClient.ID && client.IsHost == pendingClient.IsHost && client.IsServer == pendingClient.IsServer)
                {
                    removedFromPending = false;
                }
            }
            // Check if client has been added to remote list
            foreach (Client client in remoteClients)
            {
                if (client.ID == pendingClient.ID && client.IsHost == pendingClient.IsHost && client.IsServer == pendingClient.IsServer)
                {
                    addedToRemote = true;
                }
            }

            // Assert
            if (addedToRemote && removedFromPending)
            {
                Assert.Pass();
            }
            if (!addedToRemote)
            {
                Assert.Fail("Client not added to remote clients list");
            }
            if (!removedFromPending)
            {
                Assert.Fail("Client not removed from pending list");
            }
        }

        [Test()]
        public void HandleConnectionAcceptTest_LocalClientIsReceiverOfInitialRequest_And_TwoConnectionsToCopy_And_ConnectionEstablishedSuccesfully()
        {
            // Arrange
            TestNetworkManager manager = new TestNetworkManager(ConnectionType.PEER_TO_PEER, 25, 28000);
            List<Client> pendingClients = new List<Client>();
            Client pendingClient = new Client("123.123.1.2", 27000, true, false, 567, manager);
            pendingClients.Add(pendingClient);
            manager.PendingClientsInternal = pendingClients;

            string connection1IP = "155.155.2.5";
            string connection2IP = "200.200.6.3";
            byte[] data = Encoding.ASCII.GetBytes($"0/25/ACCEPT/id=567/isHost=true/isServer=false/connectionNum=2/connectionIP={connection1IP}/connectionIP={connection2IP}END");
            Packet acceptPacket = new Packet("123.125.5.5", pendingClients[0].IP, 28000, data, PacketType.ACCEPT);

            // Act
            manager.HandleConnectionAccept(acceptPacket);

            List<Client> remoteClients = manager.RemoteClients;
            pendingClients = manager.PendingClients;

            bool initialClientRemovedFromPending = true;
            bool initialClientAddedToRemote = false;

            // Check if initial client has been removed from pending list
            foreach (Client client in pendingClients)
            {
                if (client.ID == pendingClient.ID && client.IsHost == pendingClient.IsHost && client.IsServer == pendingClient.IsServer)
                {
                    initialClientRemovedFromPending = false;
                }
            }
            // Check if initial client has been added to remote list
            foreach (Client client in remoteClients)
            {
                if (client.ID == pendingClient.ID && client.IsHost == pendingClient.IsHost && client.IsServer == pendingClient.IsServer)
                {
                    initialClientAddedToRemote = true;
                }
            }

            // Check if a connection request has been sent to the two clients detailed in the accept packet


            // Assert
        }
    }
}
