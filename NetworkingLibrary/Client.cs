using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    internal class Client
    {
        string name;

        List<Connection> connections;

        string ip;

        bool isHost;

        public Client(string name, string ip, bool isHost)
        {
            this.name = name;
            this.ip = ip;
            this.isHost = isHost;

            connections = new List<Connection>();
        }

        public Client(string name, string ip)
        {
            this.name = name;
            this.ip = ip;
            this.isHost = false;

            connections = new List<Connection>();
        }

        public string Name
        {
            get { return name; }
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
    }
}
