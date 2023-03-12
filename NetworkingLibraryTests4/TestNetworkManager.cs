using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NetworkingLibrary;

namespace NetworkingLibrary.Tests
{
    internal class TestNetworkManager : NetworkManager
    {
        public TestNetworkManager(ConnectionType connectionType, int protocolID, int port) : base(connectionType, protocolID, port)
        {
        }

        public override void ClientDisconnect(int clientID)
        {
            
        }

        public override void ConnectionEstablished(Connection connection)
        {
            
        }

        public override void ConstructRemoteObject(int clientID, int objectID, Type objectType, Dictionary<string, string> properties)
        {
            
        }
    }
}
