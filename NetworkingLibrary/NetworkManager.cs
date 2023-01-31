using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    enum ConnectionType
    {
        PEER_TO_PEER,
        CLIENT_SERVER,
        DEDICATED_SERVER
    }

    public abstract class NetworkManager
    {
        List<Client> clients;
        ConnectionType connectionType;

        int hostIndex;
        int protocolID;

        Client server;

        PacketManager packetManager;

        public NetworkManager()
        {

        }

        public virtual void ConnectionRequest(Packet connectionPacket)
        {
            /*when a packet is received and determined to be an initial connection request packet,
             * this method is called. If connection succeeds, a new client is created with the information 
             * from the connection packet and added to the list of clients in the session after calling the 
             * game specific connection method to give the developer more control over what happens during a 
             * connection
             */
        }

        public virtual void ClientDisconnect(Packet disconnectPacket)
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
