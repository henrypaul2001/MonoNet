using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    public class Client
    {
        int id;
        List<Connection> connections;

        string ip;

        bool isHost;
        bool isServer;

        public Client(string ip, bool isHost, bool isServer, List<Client> otherClients)
        {
            Random rnd = new Random();
            
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

            int randomInt = rnd.Next(100, 201);
            isServer = isServer;

            this.ip = ip;
            this.isHost = isHost;

            connections = new List<Connection>();
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
    }
}
