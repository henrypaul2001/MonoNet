using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    public class Client
    {
        int id;
        int port;

        List<Connection> connections;
        Socket socket;

        string ip;

        bool isHost;
        bool isServer;

        public Client(string ip, bool isHost, bool isServer, bool isLocalClient, List<Client> otherClients, int port)
        {
            this.port = port;

            // Get all client IDs
            List<int> clientIDs = new List<int>();
            if (otherClients != null)
            {
                for (int i = 0; i < otherClients.Count(); i++)
                {
                    if (otherClients[i] != null)
                    {
                        clientIDs.Add(otherClients[i].ID);
                    }
                }
            }

            if (isLocalClient)
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // Socket will listen to packets from all IP addresses
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
            }

            // Generate unique ID for client
            id = GenerateClientID(clientIDs);

            this.isServer = isServer;
            this.ip = ip;
            this.isHost = isHost;

            connections = new List<Connection>();

            EstablishConnection(ip);
        }

        public int ID
        {
            get { return id; }
        }

        public List<Connection> Connections
        {
            get { return connections; }
        }

        public string IP
        {
            get { return ip; }
        }

        public bool IsHost
        {
            get { return isHost; }
            set { isHost = value; }
        }

        int GenerateClientID(List<int> excludedIDs)
        {
            int id;
            Random rnd = new Random();

            do
            {
                id = rnd.Next(100, 201);
            } while (excludedIDs.Contains(id));

            return id;
        }

        void EstablishConnection(string ip)
        {
            byte[] data = Encoding.ASCII.GetBytes($"REQUEST/id={id}/isHost={isHost}/isServer={isServer}");
            Packet connectionPacket = new Packet(ip, this.ip, port, data, PacketType.CONNECT);
            PacketManager.SendPacket(connectionPacket, ref socket);
        }
    }
}