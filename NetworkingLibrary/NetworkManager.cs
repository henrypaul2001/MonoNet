using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

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
        List<Client> remoteClients;
        ConnectionType connectionType;

        int hostIndex;
        int protocolID;
        int port;

        Client server;

        Client localClient;

        PacketManager packetManager;

        public NetworkManager(ConnectionType connectionType)
        {
            this.connectionType = connectionType;

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
                localClient = new Client(localIP, false, false, remoteClients);
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

        public ConnectionType ConnectionType
        {
            get { return connectionType; }
        }

        public virtual void ConnectionRequest()
        {
            /*when a packet is received and determined to be an initial connection request packet,
             * this method is called. If connection succeeds, a new client is created with the information 
             * from the connection packet and added to the list of clients in the session after calling the 
             * game specific connection method to give the developer more control over what happens during a 
             * connection
             */
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
    }
}
