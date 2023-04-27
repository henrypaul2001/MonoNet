using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    public class Client
    {
        #region stuff for unit tests
        internal List<string> IPsConnectionRequestSentTo = new List<string>();
        internal int requestConnectionCalls = 0;

        internal int acceptConnectionCalls = 0;
        #endregion

        int id;
        int port;

        SocketWrapper socket;

        NetworkManager networkManager;

        string ip;

        // Local client constructor
        internal Client(string ip, NetworkManager networkManager)
        {
            this.networkManager = networkManager;

            port = networkManager.Port;
            
            // If local client
            socket = new SocketWrapper(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp, networkManager);

            // Socket will listen to packets from all IP addresses
            socket.Bind(new IPEndPoint(IPAddress.Any, port));

            id = -1;

            networkManager.PacketManager.StartReceiving(socket, networkManager);

            this.ip = ip;
        }

        // Remote client constructor
        internal Client(string ip, int port, int id, NetworkManager networkManager)
        {
            this.networkManager = networkManager;

            this.ip = ip;
            this.id = id;
            this.port = port;
        }

        internal ref SocketWrapper Socket
        {
            get { return ref socket; }
        }

        internal int InternalID
        {
            get { return id; }
            set { id = value; }
        }

        /// <summary>
        /// The ID of the client, used to identify game objects
        /// </summary>
        public int ID
        {
            get { return id; }
        }

        /// <summary>
        /// The IP address of the client
        /// </summary>
        public string IP
        {
            get { return ip; }
        }

        /// <summary>
        /// The port number used by the client's socket
        /// </summary>
        public int Port
        {
            get { return port; }
        }

        internal byte[] RequestConnection(string ip, int portDestination)
        {
            requestConnectionCalls++;
            IPsConnectionRequestSentTo.Add(ip);
            byte[] data = Encoding.ASCII.GetBytes($"0/{networkManager.ProtocolID}/REQUEST/id={id}");
            Packet connectionPacket = new Packet(ip, this.ip, portDestination, data, PacketType.REQUEST);
            networkManager.PacketManager.SendPacket(connectionPacket, socket);
            //networkManager.PacketManager.StartReceiving(ref socket, networkManager);
            return data;
        }

        internal void AcceptConnection(Packet connectionPacket, int remoteClientID)
        {
            acceptConnectionCalls++;

            string ip = connectionPacket.IPSource;
            int destinationPort = connectionPacket.PortSource;

            List<Client> otherClients = networkManager.RemoteClients;
            int connectionNum = 0;
            if (otherClients != null)
            {
                connectionNum = otherClients.Count;
            }

            // If there are no existing connections, generate a client ID for this client
            if (connectionNum == 0)
            {
                id = networkManager.GenerateClientID(new List<int> { remoteClientID });
            }

            string payload = ($"0/{networkManager.ProtocolID}/ACCEPT/id={id}/yourID={remoteClientID}/connectionNum={connectionNum}");

            for (int i = 0; i < connectionNum; i++)
            {
                payload += $"/connection{i}IP={otherClients[i].IP}";
                payload += $"/connection{i}Port={otherClients[i].port}";
            }

            payload += "/END";

            byte[] data = Encoding.ASCII.GetBytes(payload);
            Packet acceptPacket = new Packet(ip, this.ip, destinationPort, data, PacketType.ACCEPT);
            networkManager.PacketManager.SendPacket(acceptPacket, socket);
            //networkManager.PacketManager.StartReceiving(ref socket, networkManager);
        }

        internal void Close()
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
        }

        public override string ToString()
        {
            return $"IP={IP}/PORT={Port}/ID:{ID}";
        }
    }
}