using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NetworkingLibrary
{
    public enum ConnectionType
    {
        PEER_TO_PEER,
        CLIENT_SERVER,
        DEDICATED_SERVER
    }

    public abstract class NetworkManager
    {
        // List representing currently connected clients
        List<Client> remoteClients;

        // List representing current connections
        List<Connection> connections;

        // List representing clients in the process of establishing connection (waiting for connection acknowledgement)
        List<Client> pendingClients;

        ConnectionType connectionType;

        int hostIndex;
        int protocolID;
        int port;

        Client server;

        Client localClient;

        PacketManager packetManager;

        public NetworkManager(ConnectionType connectionType, int protocolID, int port)
        {
            this.connectionType = connectionType;
            this.protocolID = protocolID;
            this.port = port;
            packetManager = new PacketManager(this);

            pendingClients = new List<Client>();
            //remoteClients = new List<Client>();
            connections = new List<Connection>();

            if (this.connectionType == ConnectionType.PEER_TO_PEER)
            {
                string localIP = null;
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        // IP is IPv4
                        localIP = ip.ToString();
                        break;
                    }
                }
                localClient = new Client(localIP, false, false, true, this);

                localClient.RequestConnection(localIP);
            }
        }

        public Client LocalClient
        {
            get { return localClient; }
        }
        public List<Client> RemoteClients
        {
            get { return remoteClients; }
        }

        public List<Client> PendingClients
        {
            get { return pendingClients; }
        }

        public ConnectionType ConnectionType
        {
            get { return connectionType; }
        }

        public int ProtocolID
        {
            get { return protocolID; }
        }

        public int Port
        {
            get { return port; }
        }

        internal PacketManager PacketManager
        {
            get { return packetManager; }
        }

        public virtual void ConnectionRequest(Packet connectionPacket)
        {
            string data = Encoding.ASCII.GetString(connectionPacket.Data);
            string[] split = data.Split('/');

            // Retrieve client info from packet
            int remoteID;
            bool parseRemoteID = int.TryParse(split[2].Substring(split[2].IndexOf('=') + 1), out remoteID);
            if (!parseRemoteID) {
                Console.WriteLine("Error parsing remoteID, id set to 1000");
                remoteID = 1000;
            }

            bool remoteIsHost;
            bool parseHostBool = bool.TryParse(split[3].Substring(split[3].IndexOf('=') + 1), out remoteIsHost);
            if (!parseHostBool)
            {
                Console.WriteLine("Error parsing remoteIsHost, value set to false");
                remoteIsHost = false;
            }

            bool remoteIsServer;
            bool parseServerBool = bool.TryParse(split[4].Substring(split[4].IndexOf('=') + 1), out remoteIsServer);
            if (!parseServerBool)
            {
                Console.WriteLine("Error parsing remoteIsServer, value set to false");
                remoteIsServer = false;
            }

            Client remoteClient = new Client(connectionPacket.IPSource, remoteIsHost, remoteIsServer, remoteID, this);
            
            pendingClients.Add(remoteClient);

            // Send connection accept to remote client
            localClient.AcceptConnection(connectionPacket.IPSource);
        }

        public virtual void ConnectionAccept(Packet acceptPacket)
        {
            string data = Encoding.ASCII.GetString(acceptPacket.Data);
            string[] split = data.Split('/');

            // Retrieve client info from packet
            int remoteID;
            bool parseRemoteID = int.TryParse(split[2].Substring(split[2].IndexOf('=') + 1), out remoteID);
            if (!parseRemoteID)
            {
                Console.WriteLine("Error parsing remoteID, id set to 1000");
                remoteID = 1000;
            }

            bool remoteIsHost;
            bool parseHostBool = bool.TryParse(split[3].Substring(split[3].IndexOf('=') + 1), out remoteIsHost);
            if (!parseHostBool)
            {
                Console.WriteLine("Error parsing remoteIsHost, value set to false");
                remoteIsHost = false;
            }

            bool remoteIsServer;
            bool parseServerBool = bool.TryParse(split[4].Substring(split[4].IndexOf('=') + 1), out remoteIsServer);
            if (!parseServerBool)
            {
                Console.WriteLine("Error parsing remoteIsServer, value set to false");
                remoteIsServer = false;
            }

            // Get IDs belonging to currently pending clients
            List<int> pendingClientIDs = new List<int>();
            foreach (Client client in pendingClients)
            {
                pendingClientIDs.Add(client.ID);
            }

            if (pendingClientIDs.Contains(remoteID))
            {
                // If this is true, the current code path is being run on the client that initially received the connection request

                // Get remote client from pending clients list
                Client remoteClient = null;
                foreach (Client client in pendingClients)
                {
                    if (client.ID == remoteID)
                    {
                        remoteClient = client;
                        break;
                    }
                }

                // Create connection
                Connection connection = new Connection(localClient, remoteClient);
                connections.Add(connection);
                Console.WriteLine($"Connection created between local: {localClient.IP} and remote: {remoteClient.IP}");

                // Remove remote client from pending list
                pendingClients.Remove(remoteClient);
            }
            else
            {
                // If false, the current code path is being run on the client that sent the initial connection request

                // Create remote client
                Client remoteClient = new Client(acceptPacket.IPSource, remoteIsHost, remoteIsServer, remoteID, this);

                // Create connection
                Connection connection = new Connection(localClient, remoteClient);
                connections.Add(connection);

                // Send connection accept back to remote client
                localClient.AcceptConnection(remoteClient.IP);
            }
        }

        public virtual void ClientDisconnect()
        {
            /*when a packet is received and determined to be a disconnect packet, this method is called.
             * Finds the corresponding client in the list of clients according to the IP address of the 
             * packet and removes that client from the list after calling the game specific disconnect 
             * method to give the developer more control over what happens during a disconnection
             */
        }

        public virtual void ClientTimeout(Client lostClient)
        {
            /*called when a client times out. Calls a game specific timeout method to give developer more
             * control over what happens when a client times out. Removes lostClient from client list
             */
        }

        public List<string> GetConnectedAddresses()
        {
            List<string> addresses = new List<string>();

            /*
            if (remoteClients != null)
            {
                foreach (Client client in remoteClients)
                {
                    addresses.Add(client.IP);
                }
            }
            */

            if (connections != null)
            {
                foreach (Connection connection in connections)
                {
                    addresses.Add(connection.RemoteClient.IP);
                }
            }

            return addresses;
        }
    }
}
