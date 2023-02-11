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
        int id;
        int port;
        int protocolID;

        //List<Connection> connections;
        Socket socket;

        NetworkManager networkManager;

        string ip;

        bool isHost;
        bool isServer;

        // NOTE FOR SILLY DUMB LITTLE STUDENT TO SELF... MAKE A LOCALCLIENT CLASS THAT DERIVES FROM THIS CLIENT CLASS YOU WILL APPRECIATE THIS MESSAGE YOU LEFT FOR YOURSELF WHEN YOU FORGET THAT YOU WERE GONNA DO THAT
        public Client(string ip, bool isHost, bool isServer, bool isLocalClient, NetworkManager networkManager)
        {
            this.networkManager = networkManager;
            List<Client> otherClients = networkManager.RemoteClients;

            port = networkManager.Port;
            protocolID = networkManager.ProtocolID;

            if (isLocalClient)
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // Socket will listen to packets from all IP addresses
                socket.Bind(new IPEndPoint(IPAddress.Any, port));

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

                // Generate unique ID for client
                id = GenerateClientID(clientIDs);
            }

            this.isServer = isServer;
            this.ip = ip;
            this.isHost = isHost;

            //connections = new List<Connection>();

            networkManager.PacketManager.StartReceiving(ref socket, networkManager);
        }

        public Client(string ip, bool isHost, bool isServer, int id, NetworkManager networkManager)
        {
            this.networkManager = networkManager;
            List<Client> otherClients = networkManager.RemoteClients;

            port = networkManager.Port;
            protocolID = networkManager.ProtocolID;

            this.isServer = isServer;
            this.ip = ip;
            this.isHost = isHost;
            this.id = id;

            //connections = new List<Connection>();
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

        internal void RequestConnection(string ip)
        {
            byte[] data = Encoding.ASCII.GetBytes($"{protocolID}/REQUEST/id={id}/isHost={isHost}/isServer={isServer}");
            Packet connectionPacket = new Packet(ip, this.ip, port, data, PacketType.CONNECT);
            networkManager.PacketManager.SendPacket(connectionPacket, ref socket);
            //networkManager.PacketManager.StartReceiving(ref socket, networkManager);
        }

        internal void AcceptConnection(string ip)
        {
            List<Client> otherClients = networkManager.RemoteClients;
            int connectionNum = 0;
            if (otherClients != null)
            {
                connectionNum = otherClients.Count;
            }
            string payload = ($"{protocolID}/ACCEPT/id={id}/isHost={isHost}/isServer={isServer}/connectionNum={connectionNum}");

            for (int i = 0; i < connectionNum; i++)
            {
                payload += $"/connection{i}IP={otherClients[i].IP}";
            }

            byte[] data = Encoding.ASCII.GetBytes(payload);
            Packet acceptPacket = new Packet(ip, this.ip, port, data, PacketType.ACCEPT);
            networkManager.PacketManager.SendPacket(acceptPacket, ref socket);
        }
    }
}