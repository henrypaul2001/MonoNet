using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Reflection;

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
        // List representing networked game objects that need to be synced
        List<Networked_GameObject> networkedObjects;

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
            remoteClients = new List<Client>();
            connections = new List<Connection>();
            networkedObjects = new List<Networked_GameObject>();

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

        public List<Networked_GameObject> NetworkedObjects
        {
            get { return networkedObjects; }
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

        public void SendGameState()
        {
            string payload = $"/SYNC/id={localClient.ID}/VARSTART/";
            for (int i = 0; i < networkedObjects.Count; i++)
            {
                // Find all networked variables using reflection
                var type = networkedObjects[i].GetType();
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => f.IsDefined(typeof(NetworkedVariable), false));
                foreach ( var field in fields )
                {
                    payload += $"{field.Name}={field.GetValue(networkedObjects[i])}/";
                }
                payload += "VAREND/";

                for (int j = 0; j < connections.Count; j++)
                {
                    // Construct packet

                    // Send packet
                }
            }
        }

        public void ConnectLocalClientToHost(string ip, int port)
        {
            localClient.RequestConnection(ip, port);
        }

        public virtual void HandleConnectionRequest(Packet connectionPacket)
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

            Client remoteClient = new Client(connectionPacket.IPSource, connectionPacket.PortSource, remoteIsHost, remoteIsServer, remoteID, this);
            
            pendingClients.Add(remoteClient);

            // Send connection accept to remote client
            localClient.AcceptConnection(connectionPacket);
        }

        public virtual void HandleConnectionAccept(Packet acceptPacket)
        {
            string data = Encoding.ASCII.GetString(acceptPacket.Data);
            string[] split = data.Split('/');

            // Retrieve client info from packet
            int remoteID;
            bool parseRemoteID = int.TryParse(split[2].Substring(split[2].IndexOf('=') + 1), out remoteID);
            if (!parseRemoteID)
            {
                Debug.WriteLine("Error parsing remoteID, id set to 1000");
                remoteID = 1000;
            }

            bool remoteIsHost;
            bool parseHostBool = bool.TryParse(split[3].Substring(split[3].IndexOf('=') + 1), out remoteIsHost);
            if (!parseHostBool)
            {
                Debug.WriteLine("Error parsing remoteIsHost, value set to false");
                remoteIsHost = false;
            }

            bool remoteIsServer;
            bool parseServerBool = bool.TryParse(split[4].Substring(split[4].IndexOf('=') + 1), out remoteIsServer);
            if (!parseServerBool)
            {
                Debug.WriteLine("Error parsing remoteIsServer, value set to false");
                remoteIsServer = false;
            }

            int remoteConnectionsNum;
            bool parseConnectionsNum = int.TryParse(split[5].Substring(split[5].IndexOf('=') + 1), out remoteConnectionsNum);
            if (!parseConnectionsNum)
            {
                Debug.WriteLine("Error parsing remoteConnectionsNum, value set to 0");
                remoteConnectionsNum = 0;
            }

            // Connect to any additional peers
            List<string> currentConnectionAddresses = GetConnectedAddresses();
            List<string> pendingConnectionAddresses = GetPendingAddresses();
            for (int i = 0; i < remoteConnectionsNum * 2; i += 2)
            {
                string remoteIP = split[6 + i].Substring(split[6 + i].IndexOf("=") + 1);
                string remotePort = split[6 + i + 1].Substring(split[6 + i + 1].IndexOf("=") + 1);
                if (!pendingConnectionAddresses.Contains(remoteIP) && !currentConnectionAddresses.Contains(remoteIP) && int.Parse(remotePort) != localClient.Port)
                {
                    localClient.RequestConnection(remoteIP, int.Parse(remotePort));
                }
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

                remoteClients.Add(remoteClient);

                // Create connection
                Connection connection = new Connection(localClient, remoteClient);
                connections.Add(connection);
                Debug.WriteLine($"Connection created between local: {localClient.IP} {localClient.Port} and remote: {remoteClient.IP} {remoteClient.Port}");
                ConnectionEstablished(connection);

                // Remove remote client from pending list
                pendingClients.Remove(remoteClient);
            }
            else
            {
                // If false, the current code path is being run on the client that sent the initial connection request

                // Create remote client
                Client remoteClient = new Client(acceptPacket.IPSource, acceptPacket.PortSource, remoteIsHost, remoteIsServer, remoteID, this);
                remoteClients.Add(remoteClient);

                // Create connection
                Connection connection = new Connection(localClient, remoteClient);
                connections.Add(connection);
                Debug.WriteLine($"Connection created between local: {localClient.IP} {localClient.Port} and remote: {remoteClient.IP} {remoteClient.Port}");
                ConnectionEstablished(connection);

                // Send connection accept back to remote client
                localClient.AcceptConnection(acceptPacket);
            }
        }

        public abstract void ConnectionEstablished(Connection connection);

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

        public List<string> GetPendingAddresses()
        {
            List<string> addresses = new List<string>();

            if (pendingClients != null)
            {
                foreach (Client client in pendingClients)
                {
                    addresses.Add(client.IP);
                }
            }
            
            return addresses;
        }
    }
}
