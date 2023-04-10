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
        //int protocolID;

        //List<Connection> connections;
        SocketWrapper socket;

        NetworkManager networkManager;

        string ip;

        //bool isHost;
        //bool isServer;

        // NOTE FOR SILLY DUMB LITTLE STUDENT TO SELF... MAKE A LOCALCLIENT CLASS THAT DERIVES FROM THIS CLIENT CLASS YOU WILL APPRECIATE THIS MESSAGE YOU LEFT FOR YOURSELF WHEN YOU FORGET THAT YOU WERE GONNA DO THAT
        // Local client constructor
        internal Client(string ip, NetworkManager networkManager)
        {
            this.networkManager = networkManager;
            //List<Client> otherClients = networkManager.RemoteClients;

            port = networkManager.Port;
            //protocolID = networkManager.ProtocolID;
            
            // If local client
            socket = new SocketWrapper(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp, networkManager);

            // Socket will listen to packets from all IP addresses
            socket.Bind(new IPEndPoint(IPAddress.Any, port));

            //List<int> clientIDs = networkManager.GetClientIDs();

            // Generate unique ID for client
            //id = GenerateClientID(clientIDs);
            id = -1;

            networkManager.PacketManager.StartReceiving(socket, networkManager);
            //end if

            //this.isServer = isServer;
            this.ip = ip;
            //this.isHost = isHost;
        }

        // Remote client constructor
        internal Client(string ip, int port, int id, NetworkManager networkManager)
        {
            this.networkManager = networkManager;
            //List<Client> otherClients = networkManager.RemoteClients;

            //protocolID = networkManager.ProtocolID;

            //this.isServer = isServer;
            this.ip = ip;
            //this.isHost = isHost;
            this.id = id;
            this.port = port;

            //connections = new List<Connection>();
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

        public int ID
        {
            get { return id; }
        }

        /*
        public List<Connection> Connections
        {
            get { return connections; }
        }
        */

        public string IP
        {
            get { return ip; }
        }

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

        public void Close()
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